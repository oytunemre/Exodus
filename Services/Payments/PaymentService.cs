using Exodus.Data;
using Exodus.Models.Dto.Payment;
using Exodus.Models.Entities;
using Exodus.Models.Enums;
using Exodus.Services.Common;
using Exodus.Services.Notifications;
using Exodus.Services.PaymentGateway;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Exodus.Services.Payments;

public class PaymentService : IPaymentService
{
    private readonly ApplicationDbContext _db;
    private readonly INotificationService _notificationService;
    private readonly IPaymentGateway? _paymentGateway;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        ApplicationDbContext db,
        INotificationService notificationService,
        ILogger<PaymentService> logger,
        IPaymentGateway? paymentGateway = null)
    {
        _db = db;
        _notificationService = notificationService;
        _logger = logger;
        _paymentGateway = paymentGateway;
    }

    public async Task<PaymentIntentResponseDto> CreateIntentAsync(CreatePaymentIntentDto dto, CancellationToken ct = default)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == dto.OrderId, ct);
        if (order is null)
            throw new NotFoundException($"Order not found. OrderId={dto.OrderId}");

        var existing = await _db.PaymentIntents.FirstOrDefaultAsync(p => p.OrderId == dto.OrderId, ct);
        if (existing is not null)
            return Map(existing);

        var intent = new PaymentIntent
        {
            OrderId = order.Id,
            Amount = order.TotalAmount,
            Currency = dto.Currency.ToUpperInvariant(),
            Method = dto.Method,
            Status = PaymentStatus.Created,
            Provider = GetProviderForMethod(dto.Method),
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            InstallmentCount = dto.InstallmentCount,
            Metadata = dto.Metadata != null ? JsonSerializer.Serialize(dto.Metadata) : null
        };

        // Handle card payments
        if (dto.CardDetails != null && (dto.Method == PaymentMethod.CreditCard || dto.Method == PaymentMethod.DebitCard))
        {
            intent.CardLast4 = dto.CardDetails.CardNumber.Length >= 4
                ? dto.CardDetails.CardNumber[^4..]
                : dto.CardDetails.CardNumber;
            intent.CardBrand = DetectCardBrand(dto.CardDetails.CardNumber);

            // Simulate 3D Secure requirement for amounts > 500
            if (order.TotalAmount > 500)
            {
                intent.Requires3DSecure = true;
                intent.ThreeDSecureUrl = $"/api/payments/{intent.Id}/3ds?returnUrl={dto.ReturnUrl}";
                intent.Status = PaymentStatus.Pending;
            }
        }

        // Calculate installment amount
        if (dto.InstallmentCount.HasValue && dto.InstallmentCount > 1)
        {
            intent.InstallmentAmount = Math.Round(order.TotalAmount / dto.InstallmentCount.Value, 2);
        }

        _db.PaymentIntents.Add(intent);
        await _db.SaveChangesAsync(ct);

        await AddEventAsync(intent.Id, PaymentStatus.Created, "payment.created",
            JsonSerializer.Serialize(new { method = intent.Method.ToString(), amount = intent.Amount }), "API", ct);

        return Map(intent);
    }

    public async Task<PaymentIntentResponseDto> GetByIdAsync(int paymentIntentId, CancellationToken ct = default)
    {
        var intent = await _db.PaymentIntents.FirstOrDefaultAsync(x => x.Id == paymentIntentId, ct);
        if (intent is null)
            throw new NotFoundException($"PaymentIntent not found. Id={paymentIntentId}");

        return Map(intent);
    }

    public async Task<PaymentIntentResponseDto> GetByOrderIdAsync(int orderId, CancellationToken ct = default)
    {
        var intent = await _db.PaymentIntents.FirstOrDefaultAsync(x => x.OrderId == orderId, ct);
        if (intent is null)
            throw new NotFoundException($"PaymentIntent not found for OrderId={orderId}");

        return Map(intent);
    }

    public async Task<PaymentIntentResponseDto> GetByExternalReferenceAsync(string externalReference, CancellationToken ct = default)
    {
        var intent = await _db.PaymentIntents.FirstOrDefaultAsync(x => x.ExternalReference == externalReference, ct);
        if (intent is null)
            throw new NotFoundException($"PaymentIntent not found for ExternalReference={externalReference}");

        return Map(intent);
    }

    public async Task<PaymentIntentResponseDto> AuthorizeAsync(int paymentIntentId, CancellationToken ct = default)
    {
        var intent = await GetIntentAsync(paymentIntentId, ct);
        EnsureCanTransitionTo(intent, PaymentStatus.Authorized);

        intent.Status = PaymentStatus.Authorized;
        intent.AuthorizedAt = DateTime.UtcNow;
        intent.ExternalReference = GenerateExternalReference();

        await _db.SaveChangesAsync(ct);
        await AddEventAsync(intent.Id, intent.Status, "payment.authorized", null, "API", ct);

        return Map(intent);
    }

    public async Task<PaymentIntentResponseDto> CaptureAsync(int paymentIntentId, string? note = null, CancellationToken ct = default)
    {
        var intent = await GetIntentAsync(paymentIntentId, ct);
        EnsureCanTransitionTo(intent, PaymentStatus.Captured);

        intent.Status = PaymentStatus.Captured;
        intent.CapturedAt = DateTime.UtcNow;
        intent.FailureReason = null;

        if (string.IsNullOrEmpty(intent.ExternalReference))
        {
            intent.ExternalReference = GenerateExternalReference();
        }

        await _db.SaveChangesAsync(ct);
        await AddEventAsync(intent.Id, intent.Status, "payment.captured",
            note != null ? JsonSerializer.Serialize(new { note }) : null, "API", ct);

        // Update order status
        await UpdateOrderPaymentStatusAsync(intent.OrderId, true, ct);

        // Send notification
        var order = await _db.Orders.FindAsync(new object[] { intent.OrderId }, ct);
        if (order != null)
        {
            await _notificationService.SendPaymentUpdateAsync(
                order.BuyerId,
                order.Id,
                "Payment Successful",
                $"Your payment of {intent.Amount:N2} {intent.Currency} has been processed successfully."
            );
        }

        return Map(intent);
    }

    public async Task<PaymentIntentResponseDto> CancelAsync(int paymentIntentId, string? reason = null, CancellationToken ct = default)
    {
        var intent = await GetIntentAsync(paymentIntentId, ct);
        EnsureCanTransitionTo(intent, PaymentStatus.Cancelled);

        intent.Status = PaymentStatus.Cancelled;
        intent.FailureReason = reason;

        await _db.SaveChangesAsync(ct);
        await AddEventAsync(intent.Id, intent.Status, "payment.cancelled",
            reason != null ? JsonSerializer.Serialize(new { reason }) : null, "API", ct);

        return Map(intent);
    }

    public async Task<PaymentIntentResponseDto> FailAsync(int paymentIntentId, string? reason = null, CancellationToken ct = default)
    {
        var intent = await GetIntentAsync(paymentIntentId, ct);
        EnsureCanTransitionTo(intent, PaymentStatus.Failed);

        intent.Status = PaymentStatus.Failed;
        intent.FailedAt = DateTime.UtcNow;
        intent.FailureReason = reason;

        await _db.SaveChangesAsync(ct);
        await AddEventAsync(intent.Id, intent.Status, "payment.failed",
            reason != null ? JsonSerializer.Serialize(new { reason }) : null, "API", ct);

        // Update order status
        await UpdateOrderPaymentStatusAsync(intent.OrderId, false, ct);

        // Send notification
        var order = await _db.Orders.FindAsync(new object[] { intent.OrderId }, ct);
        if (order != null)
        {
            await _notificationService.SendPaymentUpdateAsync(
                order.BuyerId,
                order.Id,
                "Payment Failed",
                $"Your payment could not be processed. Reason: {reason ?? "Unknown error"}"
            );
        }

        return Map(intent);
    }

    public async Task<RefundPaymentResponseDto> RefundAsync(int paymentIntentId, decimal? amount = null, string? reason = null, CancellationToken ct = default)
    {
        var intent = await GetIntentAsync(paymentIntentId, ct);

        if (intent.Status != PaymentStatus.Captured && intent.Status != PaymentStatus.PartiallyRefunded)
            throw new BadRequestException("Only captured payments can be refunded");

        var refundAmount = amount ?? (intent.Amount - intent.RefundedAmount);
        var maxRefundable = intent.Amount - intent.RefundedAmount;

        if (refundAmount <= 0 || refundAmount > maxRefundable)
            throw new BadRequestException($"Invalid refund amount. Maximum refundable: {maxRefundable:N2}");

        intent.RefundedAmount += refundAmount;
        intent.RefundedAt = DateTime.UtcNow;

        // Determine new status
        if (intent.RefundedAmount >= intent.Amount)
        {
            intent.Status = PaymentStatus.Refunded;
        }
        else
        {
            intent.Status = PaymentStatus.PartiallyRefunded;
        }

        await _db.SaveChangesAsync(ct);
        await AddEventAsync(intent.Id, intent.Status, "payment.refunded",
            JsonSerializer.Serialize(new { amount = refundAmount, reason }), "API", ct);

        // Send notification
        var order = await _db.Orders.FindAsync(new object[] { intent.OrderId }, ct);
        if (order != null)
        {
            await _notificationService.SendPaymentUpdateAsync(
                order.BuyerId,
                order.Id,
                "Refund Processed",
                $"A refund of {refundAmount:N2} {intent.Currency} has been processed."
            );
        }

        return new RefundPaymentResponseDto
        {
            PaymentIntentId = intent.Id,
            RefundedAmount = refundAmount,
            TotalRefundedAmount = intent.RefundedAmount,
            RemainingAmount = intent.Amount - intent.RefundedAmount,
            Status = intent.Status,
            ExternalReference = intent.ExternalReference,
            RefundedAt = intent.RefundedAt ?? DateTime.UtcNow
        };
    }

    public async Task<PaymentIntentResponseDto> Confirm3DSecureAsync(int paymentIntentId, string authenticationResult, CancellationToken ct = default)
    {
        var intent = await GetIntentAsync(paymentIntentId, ct);

        if (!intent.Requires3DSecure)
            throw new BadRequestException("This payment does not require 3D Secure");

        if (intent.Status != PaymentStatus.Pending)
            throw new BadRequestException("Payment is not awaiting 3D Secure confirmation");

        // Simulate 3D Secure result
        if (authenticationResult.Equals("success", StringComparison.OrdinalIgnoreCase))
        {
            intent.Status = PaymentStatus.Authorized;
            intent.AuthorizedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            await AddEventAsync(intent.Id, intent.Status, "3ds.authenticated", null, "3DSecure", ct);

            // Auto-capture after successful 3DS
            return await CaptureAsync(paymentIntentId, "Auto-captured after 3DS", ct);
        }
        else
        {
            intent.Status = PaymentStatus.Failed;
            intent.FailedAt = DateTime.UtcNow;
            intent.FailureReason = "3D Secure authentication failed";
            await _db.SaveChangesAsync(ct);
            await AddEventAsync(intent.Id, intent.Status, "3ds.failed",
                JsonSerializer.Serialize(new { result = authenticationResult }), "3DSecure", ct);
        }

        return Map(intent);
    }

    public async Task ProcessWebhookAsync(string provider, string payload, string? signature = null, CancellationToken ct = default)
    {
        // In a real implementation, verify the signature
        // For now, just parse and process the webhook

        var webhookEvent = JsonSerializer.Deserialize<WebhookEvent>(payload,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (webhookEvent == null || string.IsNullOrEmpty(webhookEvent.ExternalReference))
            throw new BadRequestException("Invalid webhook payload");

        var intent = await _db.PaymentIntents
            .FirstOrDefaultAsync(x => x.ExternalReference == webhookEvent.ExternalReference, ct);

        if (intent == null)
        {
            // Log unknown webhook - payment intent not found
            return;
        }

        // Process based on event type
        switch (webhookEvent.EventType?.ToLower())
        {
            case "payment.captured":
                await CaptureAsync(intent.Id, "Captured via webhook", ct);
                break;
            case "payment.failed":
                await FailAsync(intent.Id, webhookEvent.Message, ct);
                break;
            case "payment.refunded":
                await RefundAsync(intent.Id, webhookEvent.Amount, webhookEvent.Message, ct);
                break;
        }

        await AddEventAsync(intent.Id, intent.Status, $"webhook.{webhookEvent.EventType}",
            payload, "Webhook", ct);
    }

    public async Task<PaymentIntentResponseDto> MarkReceivedAsync(int paymentIntentId, string? note = null, CancellationToken ct = default)
    {
        return await CaptureAsync(paymentIntentId, note ?? "Manually marked as received", ct);
    }

    public Task<PaymentIntentResponseDto> SimulateSuccessAsync(int paymentIntentId, string? note = null, CancellationToken ct = default)
        => CaptureAsync(paymentIntentId, note ?? "Simulated success", ct);

    public async Task<PaymentIntentResponseDto> SimulateFailAsync(int paymentIntentId, string? reason = null, CancellationToken ct = default)
        => await FailAsync(paymentIntentId, reason ?? "Simulated failure", ct);

    public async Task<List<PaymentEventDto>> GetPaymentEventsAsync(int paymentIntentId, CancellationToken ct = default)
    {
        return await _db.PaymentEvents
            .Where(e => e.PaymentIntentId == paymentIntentId)
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new PaymentEventDto
            {
                Id = e.Id,
                PaymentIntentId = e.PaymentIntentId,
                Status = e.Status,
                EventType = e.EventType,
                PayloadJson = e.PayloadJson,
                Source = e.Source,
                CreatedAt = e.CreatedAt
            })
            .ToListAsync(ct);
    }

    // Private helpers

    private async Task<PaymentIntent> GetIntentAsync(int paymentIntentId, CancellationToken ct)
    {
        var intent = await _db.PaymentIntents.FirstOrDefaultAsync(x => x.Id == paymentIntentId, ct);
        if (intent is null)
            throw new NotFoundException($"PaymentIntent not found. Id={paymentIntentId}");
        return intent;
    }

    private static void EnsureCanTransitionTo(PaymentIntent intent, PaymentStatus targetStatus)
    {
        var validTransitions = new Dictionary<PaymentStatus, PaymentStatus[]>
        {
            { PaymentStatus.Created, new[] { PaymentStatus.Pending, PaymentStatus.Authorized, PaymentStatus.Captured, PaymentStatus.Failed, PaymentStatus.Cancelled, PaymentStatus.Expired } },
            { PaymentStatus.Pending, new[] { PaymentStatus.Authorized, PaymentStatus.Captured, PaymentStatus.Failed, PaymentStatus.Cancelled, PaymentStatus.Expired } },
            { PaymentStatus.Authorized, new[] { PaymentStatus.Captured, PaymentStatus.Cancelled, PaymentStatus.Expired } },
            { PaymentStatus.Captured, new[] { PaymentStatus.Refunded, PaymentStatus.PartiallyRefunded } },
            { PaymentStatus.PartiallyRefunded, new[] { PaymentStatus.Refunded, PaymentStatus.PartiallyRefunded } }
        };

        if (!validTransitions.TryGetValue(intent.Status, out var allowed) || !allowed.Contains(targetStatus))
        {
            throw new BadRequestException($"Cannot transition from {intent.Status} to {targetStatus}");
        }
    }

    private async Task AddEventAsync(int paymentIntentId, PaymentStatus status, string eventType, string? payloadJson, string source, CancellationToken ct)
    {
        _db.PaymentEvents.Add(new PaymentEvent
        {
            PaymentIntentId = paymentIntentId,
            Status = status,
            EventType = eventType,
            PayloadJson = payloadJson,
            Source = source
        });

        await _db.SaveChangesAsync(ct);
    }

    private async Task UpdateOrderPaymentStatusAsync(int orderId, bool success, CancellationToken ct)
    {
        var order = await _db.Orders.FindAsync(new object[] { orderId }, ct);
        if (order == null) return;

        if (success)
        {
            order.Status = OrderStatus.Processing;
            order.PaidAt = DateTime.UtcNow;
        }
        else
        {
            order.Status = OrderStatus.Failed;
        }

        await _db.SaveChangesAsync(ct);
    }

    private static PaymentIntentResponseDto Map(PaymentIntent x) => new()
    {
        Id = x.Id,
        OrderId = x.OrderId,
        Amount = x.Amount,
        RefundedAmount = x.RefundedAmount,
        Currency = x.Currency,
        Method = x.Method,
        Status = x.Status,
        Provider = x.Provider,
        ExternalReference = x.ExternalReference,
        FailureReason = x.FailureReason,
        CardLast4 = x.CardLast4,
        CardBrand = x.CardBrand,
        Requires3DSecure = x.Requires3DSecure,
        ThreeDSecureUrl = x.ThreeDSecureUrl,
        InstallmentCount = x.InstallmentCount,
        InstallmentAmount = x.InstallmentAmount,
        AuthorizedAt = x.AuthorizedAt,
        CapturedAt = x.CapturedAt,
        FailedAt = x.FailedAt,
        RefundedAt = x.RefundedAt,
        ExpiresAt = x.ExpiresAt,
        CreatedAt = x.CreatedAt
    };

    private static string GetProviderForMethod(PaymentMethod method)
    {
        return method switch
        {
            PaymentMethod.CreditCard or PaymentMethod.DebitCard or PaymentMethod.Installment => "STRIPE",
            PaymentMethod.Wallet => "PAYPAL",
            PaymentMethod.BankTransfer => "BANK",
            PaymentMethod.BuyNowPayLater => "KLARNA",
            _ => "MANUAL"
        };
    }

    private static string DetectCardBrand(string cardNumber)
    {
        if (string.IsNullOrEmpty(cardNumber)) return "Unknown";

        var cleanNumber = cardNumber.Replace(" ", "").Replace("-", "");

        if (cleanNumber.StartsWith('4')) return "Visa";
        if (cleanNumber.StartsWith('5')) return "Mastercard";
        if (cleanNumber.StartsWith("34") || cleanNumber.StartsWith("37")) return "Amex";
        if (cleanNumber.StartsWith("6011")) return "Discover";

        return "Unknown";
    }

    private static string GenerateExternalReference()
    {
        return $"PAY-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
    }

    // Gateway Methods (iyzico integration)

    public async Task<IyzicoPaymentResponseDto> ProcessWithGatewayAsync(ProcessGatewayPaymentDto dto, CancellationToken ct = default)
    {
        if (_paymentGateway == null)
            throw new InvalidOperationException("Payment gateway is not configured");

        var order = await _db.Orders
            .Include(o => o.Buyer)
            .Include(o => o.SellerOrders)
                .ThenInclude(so => so.Items)
                    .ThenInclude(i => i.Listing)
                        .ThenInclude(l => l!.Product)
            .FirstOrDefaultAsync(o => o.Id == dto.OrderId, ct);

        if (order is null)
            throw new NotFoundException($"Order not found. OrderId={dto.OrderId}");

        // Create or get existing payment intent
        var intent = await _db.PaymentIntents.FirstOrDefaultAsync(p => p.OrderId == dto.OrderId, ct);
        if (intent is null)
        {
            intent = new PaymentIntent
            {
                OrderId = order.Id,
                Amount = order.TotalAmount,
                Currency = "TRY",
                Method = PaymentMethod.CreditCard,
                Status = PaymentStatus.Created,
                Provider = _paymentGateway.ProviderName.ToUpper(),
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                InstallmentCount = dto.InstallmentCount
            };
            _db.PaymentIntents.Add(intent);
            await _db.SaveChangesAsync(ct);
        }

        // Build gateway request
        var gatewayRequest = BuildGatewayRequest(order, dto, intent);

        // Process payment
        var result = await _paymentGateway.InitiatePaymentAsync(gatewayRequest, ct);

        if (result.Success)
        {
            intent.Status = PaymentStatus.Captured;
            intent.CapturedAt = DateTime.UtcNow;
            intent.ExternalReference = result.PaymentId;
            intent.CardLast4 = result.CardLast4;
            intent.CardBrand = result.CardBrand;

            order.Status = OrderStatus.Processing;
            order.PaidAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            await AddEventAsync(intent.Id, PaymentStatus.Captured, "payment.captured.gateway",
                JsonSerializer.Serialize(new { gatewayPaymentId = result.PaymentId }), _paymentGateway.ProviderName, ct);

            await _notificationService.SendPaymentUpdateAsync(
                order.BuyerId,
                order.Id,
                "Odeme Basarili",
                $"{intent.Amount:N2} {intent.Currency} tutarindaki odemeniz basariyla tamamlandi."
            );
        }
        else
        {
            intent.Status = PaymentStatus.Failed;
            intent.FailedAt = DateTime.UtcNow;
            intent.FailureReason = result.ErrorMessage;

            await _db.SaveChangesAsync(ct);
            await AddEventAsync(intent.Id, PaymentStatus.Failed, "payment.failed.gateway",
                JsonSerializer.Serialize(new { errorCode = result.ErrorCode, errorMessage = result.ErrorMessage }), _paymentGateway.ProviderName, ct);
        }

        return new IyzicoPaymentResponseDto
        {
            Success = result.Success,
            PaymentIntentId = intent.Id,
            GatewayPaymentId = result.PaymentId,
            GatewayTransactionId = result.PaymentTransactionId,
            PaidPrice = result.PaidPrice,
            Currency = result.Currency,
            Installment = result.Installment,
            CardLast4 = result.CardLast4,
            CardBrand = result.CardBrand,
            ErrorCode = result.ErrorCode,
            ErrorMessage = result.ErrorMessage
        };
    }

    public async Task<Iyzico3DSResponseDto> Initialize3DSPaymentAsync(ProcessGatewayPaymentDto dto, CancellationToken ct = default)
    {
        if (_paymentGateway == null)
            throw new InvalidOperationException("Payment gateway is not configured");

        var order = await _db.Orders
            .Include(o => o.Buyer)
            .Include(o => o.SellerOrders)
                .ThenInclude(so => so.Items)
                    .ThenInclude(i => i.Listing)
                        .ThenInclude(l => l!.Product)
            .FirstOrDefaultAsync(o => o.Id == dto.OrderId, ct);

        if (order is null)
            throw new NotFoundException($"Order not found. OrderId={dto.OrderId}");

        // Create payment intent
        var intent = await _db.PaymentIntents.FirstOrDefaultAsync(p => p.OrderId == dto.OrderId, ct);
        if (intent is null)
        {
            intent = new PaymentIntent
            {
                OrderId = order.Id,
                Amount = order.TotalAmount,
                Currency = "TRY",
                Method = PaymentMethod.CreditCard,
                Status = PaymentStatus.Pending,
                Provider = _paymentGateway.ProviderName.ToUpper(),
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                InstallmentCount = dto.InstallmentCount,
                Requires3DSecure = true
            };
            _db.PaymentIntents.Add(intent);
            await _db.SaveChangesAsync(ct);
        }

        // Build gateway request
        var gatewayRequest = BuildGatewayRequest(order, dto, intent);
        gatewayRequest.CallbackUrl = dto.CallbackUrl;

        // Initialize 3DS
        var result = await _paymentGateway.Initiate3DSPaymentAsync(gatewayRequest, ct);

        if (result.Success)
        {
            intent.ThreeDSecureUrl = result.RedirectUrl;
            await _db.SaveChangesAsync(ct);
            await AddEventAsync(intent.Id, PaymentStatus.Pending, "3ds.initialized",
                JsonSerializer.Serialize(new { paymentId = result.PaymentId }), _paymentGateway.ProviderName, ct);
        }

        return new Iyzico3DSResponseDto
        {
            Success = result.Success,
            PaymentIntentId = intent.Id,
            ThreeDSHtmlContent = result.ThreeDSHtmlContent,
            RedirectUrl = result.RedirectUrl,
            ErrorCode = result.ErrorCode,
            ErrorMessage = result.ErrorMessage
        };
    }

    public async Task<IyzicoPaymentResponseDto> Complete3DSPaymentAsync(string paymentToken, CancellationToken ct = default)
    {
        if (_paymentGateway == null)
            throw new InvalidOperationException("Payment gateway is not configured");

        var result = await _paymentGateway.Complete3DSPaymentAsync(paymentToken, ct);

        // Find the payment intent by external reference or conversation id (which is the intent.Id)
        // First try to find by external reference, then by conversation id (intent.Id stored as string)
        var intent = await _db.PaymentIntents
            .FirstOrDefaultAsync(p => p.ExternalReference == paymentToken, ct);

        // If not found by external reference, try to parse paymentToken as conversation id (intent.Id)
        if (intent == null && int.TryParse(paymentToken, out var intentId))
        {
            intent = await _db.PaymentIntents
                .FirstOrDefaultAsync(p => p.Id == intentId && p.Status == PaymentStatus.Pending && p.Requires3DSecure, ct);
        }

        if (intent != null)
        {
            if (result.Success)
            {
                intent.Status = PaymentStatus.Captured;
                intent.CapturedAt = DateTime.UtcNow;
                intent.ExternalReference = result.PaymentId;
                intent.CardLast4 = result.CardLast4;
                intent.CardBrand = result.CardBrand;

                var order = await _db.Orders.FindAsync(new object[] { intent.OrderId }, ct);
                if (order != null)
                {
                    order.Status = OrderStatus.Processing;
                    order.PaidAt = DateTime.UtcNow;

                    await _notificationService.SendPaymentUpdateAsync(
                        order.BuyerId,
                        order.Id,
                        "Odeme Basarili",
                        $"{intent.Amount:N2} {intent.Currency} tutarindaki odemeniz 3D Secure ile tamamlandi."
                    );
                }

                await _db.SaveChangesAsync(ct);
                await AddEventAsync(intent.Id, PaymentStatus.Captured, "3ds.completed",
                    JsonSerializer.Serialize(new { paymentId = result.PaymentId }), _paymentGateway.ProviderName, ct);
            }
            else
            {
                intent.Status = PaymentStatus.Failed;
                intent.FailedAt = DateTime.UtcNow;
                intent.FailureReason = result.ErrorMessage;

                await _db.SaveChangesAsync(ct);
                await AddEventAsync(intent.Id, PaymentStatus.Failed, "3ds.failed",
                    JsonSerializer.Serialize(new { errorCode = result.ErrorCode, errorMessage = result.ErrorMessage }), _paymentGateway.ProviderName, ct);
            }
        }

        return new IyzicoPaymentResponseDto
        {
            Success = result.Success,
            PaymentIntentId = intent?.Id,
            GatewayPaymentId = result.PaymentId,
            GatewayTransactionId = result.PaymentTransactionId,
            PaidPrice = result.PaidPrice,
            Currency = result.Currency,
            Installment = result.Installment,
            CardLast4 = result.CardLast4,
            CardBrand = result.CardBrand,
            CardFamily = result.CardFamily,
            CardType = result.CardType,
            FraudStatus = result.FraudStatus,
            ErrorCode = result.ErrorCode,
            ErrorMessage = result.ErrorMessage
        };
    }

    public async Task<BinCheckResponseDto> CheckBinAsync(string binNumber, CancellationToken ct = default)
    {
        if (_paymentGateway == null)
            throw new InvalidOperationException("Payment gateway is not configured");

        var result = await _paymentGateway.CheckBinAsync(binNumber, ct);

        return new BinCheckResponseDto
        {
            Success = result.Success,
            BinNumber = result.BinNumber,
            CardType = result.CardType,
            CardAssociation = result.CardAssociation,
            CardFamily = result.CardFamily,
            BankName = result.BankName,
            BankCode = result.BankCode,
            Commercial = result.Commercial,
            ErrorCode = result.ErrorCode,
            ErrorMessage = result.ErrorMessage
        };
    }

    public async Task<InstallmentResponseDto> GetInstallmentOptionsAsync(string binNumber, decimal price, CancellationToken ct = default)
    {
        if (_paymentGateway == null)
            throw new InvalidOperationException("Payment gateway is not configured");

        var result = await _paymentGateway.GetInstallmentOptionsAsync(binNumber, price, ct);

        return new InstallmentResponseDto
        {
            Success = result.Success,
            Options = result.Options.Select(o => new InstallmentOptionDto
            {
                BankName = o.BankName,
                BankCode = o.BankCode,
                CardType = o.CardType,
                CardAssociation = o.CardAssociation,
                CardFamily = o.CardFamily,
                Force3DS = o.Force3DS,
                Details = o.Details.Select(d => new InstallmentDetailDto
                {
                    InstallmentNumber = d.InstallmentNumber,
                    TotalPrice = d.TotalPrice,
                    InstallmentPrice = d.InstallmentPrice,
                    InstallmentRate = d.InstallmentRate
                }).ToList()
            }).ToList(),
            ErrorCode = result.ErrorCode,
            ErrorMessage = result.ErrorMessage
        };
    }

    private PaymentRequest BuildGatewayRequest(Order order, ProcessGatewayPaymentDto dto, PaymentIntent intent)
    {
        var buyer = order.Buyer;
        var basketItems = new List<BasketItemInfo>();

        foreach (var sellerOrder in order.SellerOrders)
        {
            foreach (var item in sellerOrder.Items)
            {
                basketItems.Add(new BasketItemInfo
                {
                    Id = item.Id.ToString(),
                    Name = item.Listing?.Product?.ProductName ?? "Product",
                    Category1 = "Marketplace",
                    ItemType = "PHYSICAL",
                    Price = item.LineTotal
                });
            }
        }

        return new PaymentRequest
        {
            ConversationId = intent.Id.ToString(),
            Price = order.TotalAmount,
            PaidPrice = order.TotalAmount,
            Currency = "TRY",
            InstallmentCount = dto.InstallmentCount,
            BasketId = order.OrderNumber,
            PaymentMethod = PaymentMethod.CreditCard,
            Card = new CardInfo
            {
                CardHolderName = dto.Card.CardHolderName,
                CardNumber = dto.Card.CardNumber,
                ExpireMonth = dto.Card.ExpireMonth,
                ExpireYear = dto.Card.ExpireYear,
                Cvc = dto.Card.Cvc,
                RegisterCard = dto.Card.RegisterCard,
                CardToken = dto.Card.CardToken,
                CardUserKey = dto.Card.CardUserKey
            },
            Buyer = new BuyerInfo
            {
                Id = buyer?.Id.ToString() ?? "0",
                Name = buyer?.Name ?? "Guest",
                Surname = "User",
                Email = buyer?.Email ?? "guest@exodus.com",
                Phone = buyer?.Phone ?? "+905000000000",
                IdentityNumber = "11111111111",
                Ip = dto.IpAddress ?? "127.0.0.1",
                City = "Istanbul",
                Country = "Turkey",
                RegistrationAddress = order.ShippingAddressSnapshot ?? "N/A"
            },
            ShippingAddress = new AddressInfo
            {
                ContactName = buyer?.Name ?? "Guest",
                City = "Istanbul",
                Country = "Turkey",
                Address = order.ShippingAddressSnapshot ?? "N/A"
            },
            BillingAddress = new AddressInfo
            {
                ContactName = buyer?.Name ?? "Guest",
                City = "Istanbul",
                Country = "Turkey",
                Address = order.BillingAddressSnapshot ?? "N/A"
            },
            BasketItems = basketItems,
            CallbackUrl = dto.CallbackUrl
        };
    }

    private class WebhookEvent
    {
        public string? EventType { get; set; }
        public string ExternalReference { get; set; } = string.Empty;
        public decimal? Amount { get; set; }
        public string? Message { get; set; }
    }
}
