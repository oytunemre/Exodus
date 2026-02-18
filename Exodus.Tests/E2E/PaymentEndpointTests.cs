using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Exodus.Controllers;
using Exodus.Models.Dto;
using Exodus.Models.Dto.CartDto;
using Exodus.Models.Dto.ListingDto;
using Exodus.Models.Dto.Payment;
using Exodus.Models.Dto.ProductDto;
using Exodus.Models.Enums;
using FluentAssertions;
using Xunit;

namespace Exodus.Tests.E2E;

public class PaymentEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public PaymentEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Helper: creates seller product+listing, customer adds to cart, checks out, returns (client, orderId, customer).
    /// The client is authenticated as the customer after this call.
    /// </summary>
    private async Task<(HttpClient Client, int OrderId, AuthResponseDto Customer)> CreateOrderAsync(string suffix)
    {
        var client = _factory.CreateClient();

        // Seller creates product + listing
        var seller = await TestHelper.RegisterAndLoginAsSellerAsync(client, $"pay{suffix}");
        var prodDto = new AddProductDto
        {
            ProductName = $"Pay Test Product {suffix}",
            ProductDescription = "For payment tests",
            Barcodes = new List<string> { Guid.NewGuid().ToString("N") }
        };
        var prodResp = await client.PostAsJsonAsync("/api/product", prodDto, TestHelper.JsonOptions);
        var prod = await prodResp.Content.ReadFromJsonAsync<ProductResponseDto>(TestHelper.JsonOptions);

        var listingDto = new AddListingDto
        {
            ProductId = prod!.Id,
            SellerId = seller.UserId,
            Price = 100.00m,
            Stock = 50,
            Condition = ListingCondition.New
        };
        var lstResp = await client.PostAsJsonAsync("/api/listings", listingDto, TestHelper.JsonOptions);
        var listing = await lstResp.Content.ReadFromJsonAsync<ListingResponseDto>(TestHelper.JsonOptions);

        // Customer adds to cart
        var customer = await TestHelper.RegisterAndLoginAsCustomerAsync(client, $"pay{suffix}");

        await client.PostAsJsonAsync("/api/cart/add",
            new AddToCartDto { UserId = customer.UserId, ListingId = listing!.Id, Quantity = 1 },
            TestHelper.JsonOptions);

        // Create address
        var addressDto = new CreateAddressDto
        {
            Title = "Home",
            FullName = "Test Customer",
            Phone = "+905551234567",
            City = "Istanbul",
            District = "Kadikoy",
            AddressLine = "Test Mah. Test Sok. No:1"
        };
        var addrResp = await client.PostAsJsonAsync("/api/address", addressDto, TestHelper.JsonOptions);
        var address = await addrResp.Content.ReadFromJsonAsync<AddressResponseDto>(TestHelper.JsonOptions);

        // Checkout
        var orderDto = new CreateOrderDto { ShippingAddressId = address!.Id };
        var orderResp = await client.PostAsJsonAsync("/api/order/checkout", orderDto, TestHelper.JsonOptions);
        var order = await orderResp.Content.ReadFromJsonAsync<OrderDetailResponseDto>(TestHelper.JsonOptions);

        return (client, order!.Id, customer);
    }

    // ==========================================
    // CREATE INTENT
    // ==========================================

    [Fact]
    public async Task CreateIntent_WithValidOrder_ReturnsCreated()
    {
        var (client, orderId, _) = await CreateOrderAsync("ci1");

        var dto = new CreatePaymentIntentDto
        {
            OrderId = orderId,
            Method = PaymentMethod.CreditCard,
            Currency = "TRY"
        };

        var response = await client.PostAsJsonAsync("/api/payment/intents", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<PaymentIntentResponseDto>(TestHelper.JsonOptions);
        result.Should().NotBeNull();
        result!.OrderId.Should().Be(orderId);
        result.Status.Should().Be(PaymentStatus.Created);
        result.Method.Should().Be(PaymentMethod.CreditCard);
        result.Currency.Should().Be("TRY");
    }

    [Fact]
    public async Task CreateIntent_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var dto = new CreatePaymentIntentDto
        {
            OrderId = 1,
            Method = PaymentMethod.CreditCard,
            Currency = "TRY"
        };

        var response = await client.PostAsJsonAsync("/api/payment/intents", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateIntent_WithCashOnDelivery_ReturnsCreated()
    {
        var (client, orderId, _) = await CreateOrderAsync("ci2");

        var dto = new CreatePaymentIntentDto
        {
            OrderId = orderId,
            Method = PaymentMethod.CashOnDelivery,
            Currency = "TRY"
        };

        var response = await client.PostAsJsonAsync("/api/payment/intents", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<PaymentIntentResponseDto>(TestHelper.JsonOptions);
        result!.Method.Should().Be(PaymentMethod.CashOnDelivery);
        result.Provider.Should().Be("MANUAL");
    }

    [Fact]
    public async Task CreateIntent_WithCardDetails_DetectsCardBrand()
    {
        var (client, orderId, _) = await CreateOrderAsync("ci3");

        var dto = new CreatePaymentIntentDto
        {
            OrderId = orderId,
            Method = PaymentMethod.CreditCard,
            Currency = "TRY",
            CardDetails = new CardDetailsDto
            {
                CardNumber = "4111111111111111",
                ExpiryDate = "12/28",
                Cvv = "123",
                CardHolderName = "Test User"
            }
        };

        var response = await client.PostAsJsonAsync("/api/payment/intents", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<PaymentIntentResponseDto>(TestHelper.JsonOptions);
        result!.CardLast4.Should().Be("1111");
        result.CardBrand.Should().Be("Visa");
    }

    [Fact]
    public async Task CreateIntent_DuplicateForSameOrder_ReturnsExistingIntent()
    {
        var (client, orderId, _) = await CreateOrderAsync("ci4");

        var dto = new CreatePaymentIntentDto
        {
            OrderId = orderId,
            Method = PaymentMethod.CreditCard,
            Currency = "TRY"
        };

        var resp1 = await client.PostAsJsonAsync("/api/payment/intents", dto, TestHelper.JsonOptions);
        var intent1 = await resp1.Content.ReadFromJsonAsync<PaymentIntentResponseDto>(TestHelper.JsonOptions);

        var resp2 = await client.PostAsJsonAsync("/api/payment/intents", dto, TestHelper.JsonOptions);
        var intent2 = await resp2.Content.ReadFromJsonAsync<PaymentIntentResponseDto>(TestHelper.JsonOptions);

        intent1!.Id.Should().Be(intent2!.Id);
    }

    [Fact]
    public async Task CreateIntent_WithInstallments_CalculatesInstallmentAmount()
    {
        var (client, orderId, _) = await CreateOrderAsync("ci5");

        var dto = new CreatePaymentIntentDto
        {
            OrderId = orderId,
            Method = PaymentMethod.Installment,
            Currency = "TRY",
            InstallmentCount = 3
        };

        var response = await client.PostAsJsonAsync("/api/payment/intents", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<PaymentIntentResponseDto>(TestHelper.JsonOptions);
        result!.InstallmentCount.Should().Be(3);
        result.InstallmentAmount.Should().BeGreaterThan(0);
    }

    // ==========================================
    // GET INTENT
    // ==========================================

    [Fact]
    public async Task GetById_ExistingIntent_ReturnsOk()
    {
        var (client, orderId, _) = await CreateOrderAsync("gbi1");

        // Create intent
        var createDto = new CreatePaymentIntentDto
        {
            OrderId = orderId,
            Method = PaymentMethod.CreditCard,
            Currency = "TRY"
        };
        var createResp = await client.PostAsJsonAsync("/api/payment/intents", createDto, TestHelper.JsonOptions);
        var created = await createResp.Content.ReadFromJsonAsync<PaymentIntentResponseDto>(TestHelper.JsonOptions);

        // Get by ID
        var response = await client.GetAsync($"/api/payment/intents/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaymentIntentResponseDto>(TestHelper.JsonOptions);
        result!.Id.Should().Be(created.Id);
        result.OrderId.Should().Be(orderId);
    }

    [Fact]
    public async Task GetByOrderId_ExistingIntent_ReturnsOk()
    {
        var (client, orderId, _) = await CreateOrderAsync("gbo1");

        var createDto = new CreatePaymentIntentDto
        {
            OrderId = orderId,
            Method = PaymentMethod.CreditCard,
            Currency = "TRY"
        };
        await client.PostAsJsonAsync("/api/payment/intents", createDto, TestHelper.JsonOptions);

        var response = await client.GetAsync($"/api/payment/order/{orderId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaymentIntentResponseDto>(TestHelper.JsonOptions);
        result!.OrderId.Should().Be(orderId);
    }

    [Fact]
    public async Task GetById_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/payment/intents/1");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ==========================================
    // AUTHORIZE
    // ==========================================

    [Fact]
    public async Task Authorize_CreatedIntent_ReturnsAuthorized()
    {
        var (client, orderId, _) = await CreateOrderAsync("auth1");

        var createDto = new CreatePaymentIntentDto
        {
            OrderId = orderId,
            Method = PaymentMethod.CreditCard,
            Currency = "TRY"
        };
        var createResp = await client.PostAsJsonAsync("/api/payment/intents", createDto, TestHelper.JsonOptions);
        var created = await createResp.Content.ReadFromJsonAsync<PaymentIntentResponseDto>(TestHelper.JsonOptions);

        var response = await client.PostAsync($"/api/payment/intents/{created!.Id}/authorize", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaymentIntentResponseDto>(TestHelper.JsonOptions);
        result!.Status.Should().Be(PaymentStatus.Authorized);
        result.AuthorizedAt.Should().NotBeNull();
        result.ExternalReference.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Authorize_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/api/payment/intents/1/authorize", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ==========================================
    // CAPTURE
    // ==========================================

    [Fact]
    public async Task Capture_AuthorizedIntent_ReturnsCaptured()
    {
        var (client, orderId, _) = await CreateOrderAsync("cap1");

        var createDto = new CreatePaymentIntentDto
        {
            OrderId = orderId,
            Method = PaymentMethod.CreditCard,
            Currency = "TRY"
        };
        var createResp = await client.PostAsJsonAsync("/api/payment/intents", createDto, TestHelper.JsonOptions);
        var created = await createResp.Content.ReadFromJsonAsync<PaymentIntentResponseDto>(TestHelper.JsonOptions);

        // Authorize first
        await client.PostAsync($"/api/payment/intents/{created!.Id}/authorize", null);

        // Capture
        var captureDto = new CapturePaymentDto { Note = "Test capture" };
        var response = await client.PostAsJsonAsync($"/api/payment/intents/{created.Id}/capture", captureDto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaymentIntentResponseDto>(TestHelper.JsonOptions);
        result!.Status.Should().Be(PaymentStatus.Captured);
        result.CapturedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Capture_CreatedIntent_ReturnsCaptured()
    {
        var (client, orderId, _) = await CreateOrderAsync("cap2");

        var createDto = new CreatePaymentIntentDto
        {
            OrderId = orderId,
            Method = PaymentMethod.CashOnDelivery,
            Currency = "TRY"
        };
        var createResp = await client.PostAsJsonAsync("/api/payment/intents", createDto, TestHelper.JsonOptions);
        var created = await createResp.Content.ReadFromJsonAsync<PaymentIntentResponseDto>(TestHelper.JsonOptions);

        // Direct capture from Created state (allowed per state machine)
        var captureDto = new CapturePaymentDto { Note = "Direct capture" };
        var response = await client.PostAsJsonAsync($"/api/payment/intents/{created!.Id}/capture", captureDto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaymentIntentResponseDto>(TestHelper.JsonOptions);
        result!.Status.Should().Be(PaymentStatus.Captured);
    }

    // ==========================================
    // CANCEL
    // ==========================================

    [Fact]
    public async Task Cancel_CreatedIntent_ReturnsCancelled()
    {
        var (client, orderId, _) = await CreateOrderAsync("cnl1");

        var createDto = new CreatePaymentIntentDto
        {
            OrderId = orderId,
            Method = PaymentMethod.CreditCard,
            Currency = "TRY"
        };
        var createResp = await client.PostAsJsonAsync("/api/payment/intents", createDto, TestHelper.JsonOptions);
        var created = await createResp.Content.ReadFromJsonAsync<PaymentIntentResponseDto>(TestHelper.JsonOptions);

        var cancelDto = new CancelPaymentDto { Reason = "Customer changed mind" };
        var response = await client.PostAsJsonAsync($"/api/payment/intents/{created!.Id}/cancel", cancelDto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaymentIntentResponseDto>(TestHelper.JsonOptions);
        result!.Status.Should().Be(PaymentStatus.Cancelled);
        result.FailureReason.Should().Be("Customer changed mind");
    }

    [Fact]
    public async Task Cancel_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var cancelDto = new CancelPaymentDto { Reason = "Test" };
        var response = await client.PostAsJsonAsync("/api/payment/intents/1/cancel", cancelDto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ==========================================
    // REFUND
    // ==========================================

    [Fact]
    public async Task Refund_CapturedIntent_FullRefund_ReturnsRefunded()
    {
        var (client, orderId, _) = await CreateOrderAsync("ref1");

        // Create and capture
        var createDto = new CreatePaymentIntentDto
        {
            OrderId = orderId,
            Method = PaymentMethod.CreditCard,
            Currency = "TRY"
        };
        var createResp = await client.PostAsJsonAsync("/api/payment/intents", createDto, TestHelper.JsonOptions);
        var created = await createResp.Content.ReadFromJsonAsync<PaymentIntentResponseDto>(TestHelper.JsonOptions);

        // Authorize and capture
        await client.PostAsync($"/api/payment/intents/{created!.Id}/authorize", null);
        await client.PostAsJsonAsync($"/api/payment/intents/{created.Id}/capture",
            new CapturePaymentDto { Note = "Captured" }, TestHelper.JsonOptions);

        // Full refund
        var refundDto = new RefundPaymentDto { Reason = "Customer return" };
        var response = await client.PostAsJsonAsync($"/api/payment/intents/{created.Id}/refund", refundDto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<RefundPaymentResponseDto>(TestHelper.JsonOptions);
        result!.Status.Should().Be(PaymentStatus.Refunded);
        result.RefundedAmount.Should().Be(created.Amount);
        result.RemainingAmount.Should().Be(0);
    }

    [Fact]
    public async Task Refund_CapturedIntent_PartialRefund_ReturnsPartiallyRefunded()
    {
        var (client, orderId, _) = await CreateOrderAsync("ref2");

        // Create and capture
        var createDto = new CreatePaymentIntentDto
        {
            OrderId = orderId,
            Method = PaymentMethod.CreditCard,
            Currency = "TRY"
        };
        var createResp = await client.PostAsJsonAsync("/api/payment/intents", createDto, TestHelper.JsonOptions);
        var created = await createResp.Content.ReadFromJsonAsync<PaymentIntentResponseDto>(TestHelper.JsonOptions);

        await client.PostAsync($"/api/payment/intents/{created!.Id}/authorize", null);
        await client.PostAsJsonAsync($"/api/payment/intents/{created.Id}/capture",
            new CapturePaymentDto { Note = "Captured" }, TestHelper.JsonOptions);

        // Partial refund
        var refundDto = new RefundPaymentDto { Amount = 30.00m, Reason = "Partial return" };
        var response = await client.PostAsJsonAsync($"/api/payment/intents/{created.Id}/refund", refundDto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<RefundPaymentResponseDto>(TestHelper.JsonOptions);
        result!.Status.Should().Be(PaymentStatus.PartiallyRefunded);
        result.RefundedAmount.Should().Be(30.00m);
        result.RemainingAmount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Refund_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var refundDto = new RefundPaymentDto { Reason = "Test" };
        var response = await client.PostAsJsonAsync("/api/payment/intents/1/refund", refundDto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ==========================================
    // SIMULATE SUCCESS / FAIL
    // ==========================================

    [Fact]
    public async Task SimulateSuccess_CreatedIntent_ReturnsCaptured()
    {
        var (client, orderId, _) = await CreateOrderAsync("sim1");

        var createDto = new CreatePaymentIntentDto
        {
            OrderId = orderId,
            Method = PaymentMethod.CreditCard,
            Currency = "TRY"
        };
        var createResp = await client.PostAsJsonAsync("/api/payment/intents", createDto, TestHelper.JsonOptions);
        var created = await createResp.Content.ReadFromJsonAsync<PaymentIntentResponseDto>(TestHelper.JsonOptions);

        var simDto = new SimulatePaymentDto { Reason = "Test simulation" };
        var response = await client.PostAsJsonAsync($"/api/payment/intents/{created!.Id}/simulate-success", simDto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaymentIntentResponseDto>(TestHelper.JsonOptions);
        result!.Status.Should().Be(PaymentStatus.Captured);
    }

    [Fact]
    public async Task SimulateFail_CreatedIntent_ReturnsFailed()
    {
        var (client, orderId, _) = await CreateOrderAsync("sim2");

        var createDto = new CreatePaymentIntentDto
        {
            OrderId = orderId,
            Method = PaymentMethod.CreditCard,
            Currency = "TRY"
        };
        var createResp = await client.PostAsJsonAsync("/api/payment/intents", createDto, TestHelper.JsonOptions);
        var created = await createResp.Content.ReadFromJsonAsync<PaymentIntentResponseDto>(TestHelper.JsonOptions);

        var simDto = new SimulatePaymentDto { Reason = "Test failure" };
        var response = await client.PostAsJsonAsync($"/api/payment/intents/{created!.Id}/simulate-fail", simDto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaymentIntentResponseDto>(TestHelper.JsonOptions);
        result!.Status.Should().Be(PaymentStatus.Failed);
        result.FailedAt.Should().NotBeNull();
    }

    // ==========================================
    // PAYMENT EVENTS
    // ==========================================

    [Fact]
    public async Task GetEvents_AfterCreateAndAuthorize_ReturnsEvents()
    {
        var (client, orderId, _) = await CreateOrderAsync("evt1");

        var createDto = new CreatePaymentIntentDto
        {
            OrderId = orderId,
            Method = PaymentMethod.CreditCard,
            Currency = "TRY"
        };
        var createResp = await client.PostAsJsonAsync("/api/payment/intents", createDto, TestHelper.JsonOptions);
        var created = await createResp.Content.ReadFromJsonAsync<PaymentIntentResponseDto>(TestHelper.JsonOptions);

        await client.PostAsync($"/api/payment/intents/{created!.Id}/authorize", null);

        var response = await client.GetAsync($"/api/payment/intents/{created.Id}/events");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var events = await response.Content.ReadFromJsonAsync<List<PaymentEventDto>>(TestHelper.JsonOptions);
        events.Should().NotBeNull();
        events!.Count.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task GetEvents_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/payment/intents/1/events");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ==========================================
    // MARK RECEIVED (Admin only)
    // ==========================================

    [Fact]
    public async Task MarkReceived_AsAdmin_ReturnsOk()
    {
        var (client, orderId, _) = await CreateOrderAsync("mr1");

        var createDto = new CreatePaymentIntentDto
        {
            OrderId = orderId,
            Method = PaymentMethod.BankTransfer,
            Currency = "TRY"
        };
        var createResp = await client.PostAsJsonAsync("/api/payment/intents", createDto, TestHelper.JsonOptions);
        var created = await createResp.Content.ReadFromJsonAsync<PaymentIntentResponseDto>(TestHelper.JsonOptions);

        // Switch to admin
        await TestHelper.RegisterAndLoginAsAdminAsync(client, "mr1");

        var markDto = new MarkPaymentReceivedDto { Note = "Bank transfer received" };
        var response = await client.PostAsJsonAsync($"/api/payment/intents/{created!.Id}/mark-received", markDto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaymentIntentResponseDto>(TestHelper.JsonOptions);
        result!.Status.Should().Be(PaymentStatus.Captured);
    }

    [Fact]
    public async Task MarkReceived_AsCustomer_ReturnsForbidden()
    {
        var (client, orderId, _) = await CreateOrderAsync("mr2");

        var createDto = new CreatePaymentIntentDto
        {
            OrderId = orderId,
            Method = PaymentMethod.BankTransfer,
            Currency = "TRY"
        };
        var createResp = await client.PostAsJsonAsync("/api/payment/intents", createDto, TestHelper.JsonOptions);
        var created = await createResp.Content.ReadFromJsonAsync<PaymentIntentResponseDto>(TestHelper.JsonOptions);

        var markDto = new MarkPaymentReceivedDto { Note = "Should fail" };
        var response = await client.PostAsJsonAsync($"/api/payment/intents/{created!.Id}/mark-received", markDto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ==========================================
    // WEBHOOK
    // ==========================================

    [Fact]
    public async Task Webhook_ValidPayload_ReturnsOk()
    {
        var client = _factory.CreateClient();

        var webhookDto = new ProcessWebhookDto
        {
            Payload = JsonSerializer.Serialize(new
            {
                EventType = "payment.captured",
                ExternalReference = "NON-EXISTENT-REF",
                Amount = 100.00m,
                Message = "Payment completed"
            })
        };

        // Webhook endpoint is AllowAnonymous
        var response = await client.PostAsJsonAsync("/api/payment/webhook/iyzico", webhookDto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ==========================================
    // GATEWAY: BIN CHECK
    // ==========================================

    [Fact]
    public async Task CheckBin_ValidBinNumber_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsCustomerAsync(client, "bin1");

        var response = await client.GetAsync("/api/payment/gateway/bin/411111");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<BinCheckResponseDto>(TestHelper.JsonOptions);
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.BinNumber.Should().Be("411111");
    }

    [Fact]
    public async Task CheckBin_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/payment/gateway/bin/411111");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ==========================================
    // GATEWAY: INSTALLMENT OPTIONS
    // ==========================================

    [Fact]
    public async Task GetInstallmentOptions_ValidParams_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsCustomerAsync(client, "inst1");

        var response = await client.GetAsync("/api/payment/gateway/installments?binNumber=411111&price=1000");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<InstallmentResponseDto>(TestHelper.JsonOptions);
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task GetInstallmentOptions_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/payment/gateway/installments?binNumber=411111&price=1000");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ==========================================
    // GATEWAY: PROCESS (non-3DS)
    // ==========================================

    [Fact]
    public async Task ProcessWithGateway_ValidOrder_ReturnsSuccess()
    {
        var (client, orderId, _) = await CreateOrderAsync("gw1");

        var dto = new ProcessGatewayPaymentDto
        {
            OrderId = orderId,
            Card = new GatewayCardDto
            {
                CardHolderName = "Test User",
                CardNumber = "4111111111111111",
                ExpireMonth = "12",
                ExpireYear = "2028",
                Cvc = "123"
            },
            Use3DSecure = false
        };

        var response = await client.PostAsJsonAsync("/api/payment/gateway/process", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<IyzicoPaymentResponseDto>(TestHelper.JsonOptions);
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.PaymentIntentId.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ProcessWithGateway_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var dto = new ProcessGatewayPaymentDto
        {
            OrderId = 1,
            Card = new GatewayCardDto
            {
                CardHolderName = "Test",
                CardNumber = "4111111111111111",
                ExpireMonth = "12",
                ExpireYear = "2028",
                Cvc = "123"
            }
        };

        var response = await client.PostAsJsonAsync("/api/payment/gateway/process", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ==========================================
    // STATE MACHINE: INVALID TRANSITIONS
    // ==========================================

    [Fact]
    public async Task Capture_CancelledIntent_ReturnsBadRequest()
    {
        var (client, orderId, _) = await CreateOrderAsync("sm1");

        var createDto = new CreatePaymentIntentDto
        {
            OrderId = orderId,
            Method = PaymentMethod.CreditCard,
            Currency = "TRY"
        };
        var createResp = await client.PostAsJsonAsync("/api/payment/intents", createDto, TestHelper.JsonOptions);
        var created = await createResp.Content.ReadFromJsonAsync<PaymentIntentResponseDto>(TestHelper.JsonOptions);

        // Cancel first
        await client.PostAsJsonAsync($"/api/payment/intents/{created!.Id}/cancel",
            new CancelPaymentDto { Reason = "Test" }, TestHelper.JsonOptions);

        // Try to capture cancelled intent
        var captureDto = new CapturePaymentDto { Note = "Should fail" };
        var response = await client.PostAsJsonAsync($"/api/payment/intents/{created.Id}/capture", captureDto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ==========================================
    // FULL PAYMENT LIFECYCLE
    // ==========================================

    [Fact]
    public async Task FullLifecycle_CreateAuthorizeCapture_UpdatesOrderStatus()
    {
        var (client, orderId, _) = await CreateOrderAsync("lc1");

        // Create
        var createDto = new CreatePaymentIntentDto
        {
            OrderId = orderId,
            Method = PaymentMethod.CreditCard,
            Currency = "TRY"
        };
        var createResp = await client.PostAsJsonAsync("/api/payment/intents", createDto, TestHelper.JsonOptions);
        var created = await createResp.Content.ReadFromJsonAsync<PaymentIntentResponseDto>(TestHelper.JsonOptions);
        created!.Status.Should().Be(PaymentStatus.Created);

        // Authorize
        var authResp = await client.PostAsync($"/api/payment/intents/{created.Id}/authorize", null);
        var authorized = await authResp.Content.ReadFromJsonAsync<PaymentIntentResponseDto>(TestHelper.JsonOptions);
        authorized!.Status.Should().Be(PaymentStatus.Authorized);

        // Capture
        var captureResp = await client.PostAsJsonAsync($"/api/payment/intents/{created.Id}/capture",
            new CapturePaymentDto { Note = "Lifecycle capture" }, TestHelper.JsonOptions);
        var captured = await captureResp.Content.ReadFromJsonAsync<PaymentIntentResponseDto>(TestHelper.JsonOptions);
        captured!.Status.Should().Be(PaymentStatus.Captured);

        // Verify order status updated to Processing
        var orderResp = await client.GetAsync($"/api/order/{orderId}");
        orderResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var order = await orderResp.Content.ReadFromJsonAsync<OrderDetailResponseDto>(TestHelper.JsonOptions);
        order!.Status.Should().Be(OrderStatus.Processing);

        // Verify events logged
        var eventsResp = await client.GetAsync($"/api/payment/intents/{created.Id}/events");
        var events = await eventsResp.Content.ReadFromJsonAsync<List<PaymentEventDto>>(TestHelper.JsonOptions);
        events!.Count.Should().BeGreaterThanOrEqualTo(3); // created, authorized, captured
    }

    [Fact]
    public async Task FullLifecycle_CreateAuthorizeCaptureRefund()
    {
        var (client, orderId, _) = await CreateOrderAsync("lc2");

        // Create
        var createDto = new CreatePaymentIntentDto
        {
            OrderId = orderId,
            Method = PaymentMethod.CreditCard,
            Currency = "TRY"
        };
        var createResp = await client.PostAsJsonAsync("/api/payment/intents", createDto, TestHelper.JsonOptions);
        var created = await createResp.Content.ReadFromJsonAsync<PaymentIntentResponseDto>(TestHelper.JsonOptions);

        // Authorize -> Capture
        await client.PostAsync($"/api/payment/intents/{created!.Id}/authorize", null);
        await client.PostAsJsonAsync($"/api/payment/intents/{created.Id}/capture",
            new CapturePaymentDto(), TestHelper.JsonOptions);

        // Partial refund
        var refund1 = await client.PostAsJsonAsync($"/api/payment/intents/{created.Id}/refund",
            new RefundPaymentDto { Amount = 20.00m, Reason = "Partial return" }, TestHelper.JsonOptions);
        var refund1Result = await refund1.Content.ReadFromJsonAsync<RefundPaymentResponseDto>(TestHelper.JsonOptions);
        refund1Result!.Status.Should().Be(PaymentStatus.PartiallyRefunded);

        // Refund remaining
        var refund2 = await client.PostAsJsonAsync($"/api/payment/intents/{created.Id}/refund",
            new RefundPaymentDto { Reason = "Full return" }, TestHelper.JsonOptions);
        var refund2Result = await refund2.Content.ReadFromJsonAsync<RefundPaymentResponseDto>(TestHelper.JsonOptions);
        refund2Result!.Status.Should().Be(PaymentStatus.Refunded);
        refund2Result.RemainingAmount.Should().Be(0);
    }
}
