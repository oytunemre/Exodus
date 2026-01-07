using FarmazonDemo.Data;
using FarmazonDemo.Models.Dto.Payment;
using FarmazonDemo.Models.Entities;
using FarmazonDemo.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace FarmazonDemo.Services.Payments;

public class PaymentService : IPaymentService
{
    private readonly ApplicationDbContext _db;

    public PaymentService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<PaymentIntentResponseDto> CreateIntentAsync(CreatePaymentIntentDto dto, CancellationToken ct = default)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == dto.OrderId, ct);
        if (order is null)
            throw new InvalidOperationException($"Order not found. OrderId={dto.OrderId}");

        var existing = await _db.PaymentIntents.FirstOrDefaultAsync(p => p.OrderId == dto.OrderId, ct);
        if (existing is not null)
            return Map(existing);

        var intent = new PaymentIntent
        {
            OrderId = order.Id,
            Amount = order.Total,                 // ✅ client’tan değil order’dan
            Currency = dto.Currency.ToUpperInvariant(),
            Method = dto.Method,
            Status = PaymentStatus.Created,
            Provider = "MANUAL",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.PaymentIntents.Add(intent);
        await _db.SaveChangesAsync(ct);

        await AddEventAsync(intent.Id, PaymentStatus.Created, $"{{\"note\":\"intent created\",\"method\":\"{intent.Method}\"}}", ct);

        return Map(intent);
    }

    public async Task<PaymentIntentResponseDto> GetByOrderIdAsync(int orderId, CancellationToken ct = default)
    {
        var intent = await _db.PaymentIntents.FirstOrDefaultAsync(x => x.OrderId == orderId, ct);
        if (intent is null)
            throw new InvalidOperationException($"PaymentIntent not found for OrderId={orderId}");

        return Map(intent);
    }

    public async Task<PaymentIntentResponseDto> MarkReceivedAsync(int paymentIntentId, string? note, CancellationToken ct = default)
    {
        var intent = await _db.PaymentIntents.FirstOrDefaultAsync(x => x.Id == paymentIntentId, ct);
        if (intent is null)
            throw new InvalidOperationException($"PaymentIntent not found. Id={paymentIntentId}");

        EnsureMutable(intent);

        intent.Status = PaymentStatus.Captured;
        intent.FailureReason = null;
        intent.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        await AddEventAsync(intent.Id, intent.Status, $"{{\"note\":\"mark received\",\"detail\":\"{Escape(note)}\"}}", ct);

        return Map(intent);
    }

    public Task<PaymentIntentResponseDto> SimulateSuccessAsync(int paymentIntentId, string? note, CancellationToken ct = default)
        => MarkReceivedAsync(paymentIntentId, note ?? "simulate success", ct);

    public async Task<PaymentIntentResponseDto> SimulateFailAsync(int paymentIntentId, string? reason, CancellationToken ct = default)
    {
        var intent = await _db.PaymentIntents.FirstOrDefaultAsync(x => x.Id == paymentIntentId, ct);
        if (intent is null)
            throw new InvalidOperationException($"PaymentIntent not found. Id={paymentIntentId}");

        EnsureMutable(intent);

        intent.Status = PaymentStatus.Failed;
        intent.FailureReason = reason?.Trim();
        intent.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        await AddEventAsync(intent.Id, intent.Status, $"{{\"note\":\"simulate fail\",\"reason\":\"{Escape(reason)}\"}}", ct);

        return Map(intent);
    }

    private static void EnsureMutable(PaymentIntent intent)
    {
        if (intent.Status is PaymentStatus.Captured or PaymentStatus.Cancelled)
            throw new InvalidOperationException($"PaymentIntent is not mutable. CurrentStatus={intent.Status}");
    }

    private async Task AddEventAsync(int paymentIntentId, PaymentStatus status, string? payloadJson, CancellationToken ct)
    {
        _db.PaymentEvents.Add(new PaymentEvent
        {
            PaymentIntentId = paymentIntentId,
            Status = status,
            PayloadJson = payloadJson,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);
    }

    private static PaymentIntentResponseDto Map(PaymentIntent x) => new()
    {
        Id = x.Id,
        OrderId = x.OrderId,
        Amount = x.Amount,
        Currency = x.Currency,
        Method = x.Method,
        Status = x.Status,
        Provider = x.Provider,
        ExternalReference = x.ExternalReference,
        FailureReason = x.FailureReason
    };

    private static string Escape(string? s)
        => string.IsNullOrWhiteSpace(s) ? "" : s.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
