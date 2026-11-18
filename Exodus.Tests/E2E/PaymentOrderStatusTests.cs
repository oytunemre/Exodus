using System.Net;
using System.Net.Http.Json;
using Exodus.Models.Dto;
using Exodus.Models.Dto.CartDto;
using Exodus.Models.Dto.ListingDto;
using Exodus.Models.Dto.Payment;
using Exodus.Models.Dto.ProductDto;
using Exodus.Models.Enums;
using FluentAssertions;
using Xunit;

namespace Exodus.Tests.E2E;

/// <summary>
/// Payment success/failure must respect the order's current status: it only moves a Pending order
/// forward, never regresses an order the sellers already advanced, and never revives a cancelled one.
/// </summary>
public class PaymentOrderStatusTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public PaymentOrderStatusTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private sealed record Scenario(AuthResponseDto Seller, AuthResponseDto Customer, int OrderId, int SellerOrderId, int IntentId);

    /// <summary>
    /// One seller, one customer, one order; a payment intent with the given method (not yet paid).
    /// <paramref name="threeDSecureCard"/> with a price over 500 creates a Pending intent awaiting 3DS.
    /// </summary>
    private async Task<Scenario> CreateScenarioAsync(
        PaymentMethod method = PaymentMethod.CashOnDelivery, decimal price = 100m, bool threeDSecureCard = false)
    {
        var s = Guid.NewGuid().ToString("N")[..8];
        var setup = _factory.CreateClient();
        var seller = await TestHelper.RegisterAndLoginAsSellerAsync(setup, $"pos{s}");

        var prodResp = await setup.PostAsJsonAsync("/api/product", new AddProductDto
        {
            ProductName = $"PayStatus Product {s}",
            ProductDescription = "Payment order status tests",
            Barcodes = new List<string> { Guid.NewGuid().ToString("N") }
        }, TestHelper.JsonOptions);
        var prod = await prodResp.Content.ReadFromJsonAsync<ProductResponseDto>(TestHelper.JsonOptions);

        var lstResp = await setup.PostAsJsonAsync("/api/listings", new AddListingDto
        {
            ProductId = prod!.Id,
            SellerId = seller.UserId,
            Price = price,
            Stock = 50,
            Condition = ListingCondition.New
        }, TestHelper.JsonOptions);
        var listing = await lstResp.Content.ReadFromJsonAsync<ListingResponseDto>(TestHelper.JsonOptions);

        var client = _factory.CreateClient();
        var customer = await TestHelper.RegisterAndLoginAsCustomerAsync(client, $"pos{s}");
        await client.PostAsJsonAsync("/api/cart/add",
            new AddToCartDto { UserId = customer.UserId, ListingId = listing!.Id, Quantity = 1 }, TestHelper.JsonOptions);

        var addrResp = await client.PostAsJsonAsync("/api/address", new CreateAddressDto
        {
            Title = "Home",
            FullName = "Test Customer",
            Phone = "+905551234567",
            City = "Istanbul",
            District = "Kadikoy",
            AddressLine = "Test Mah. Test Sok. No:1"
        }, TestHelper.JsonOptions);
        var address = await addrResp.Content.ReadFromJsonAsync<AddressResponseDto>(TestHelper.JsonOptions);

        var orderResp = await client.PostAsJsonAsync("/api/order/checkout",
            new CreateOrderDto { ShippingAddressId = address!.Id }, TestHelper.JsonOptions);
        orderResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var order = await orderResp.Content.ReadFromJsonAsync<OrderDetailResponseDto>(TestHelper.JsonOptions);

        var payResp = await client.PostAsJsonAsync("/api/payment/intents", new CreatePaymentIntentDto
        {
            OrderId = order!.Id,
            Method = method,
            Currency = "TRY",
            CardDetails = threeDSecureCard
                ? new CardDetailsDto { CardNumber = "5528790000000008", ExpiryDate = "12/30", Cvv = "123", CardHolderName = "Test Customer" }
                : null
        }, TestHelper.JsonOptions);
        payResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var intent = await payResp.Content.ReadFromJsonAsync<PaymentIntentResponseDto>(TestHelper.JsonOptions);

        return new Scenario(seller, customer, order.Id, order.SellerOrders.Single().Id, intent!.Id);
    }

    private HttpClient ClientFor(AuthResponseDto user)
    {
        var client = _factory.CreateClient();
        TestHelper.SetAuthToken(client, user.Token!);
        return client;
    }

    private async Task<OrderDetailResponseDto> GetOrderAsync(Scenario sc)
    {
        var resp = await ClientFor(sc.Customer).GetAsync($"/api/order/{sc.OrderId}");
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<OrderDetailResponseDto>(TestHelper.JsonOptions))!;
    }

    private async Task<PaymentIntentResponseDto> GetIntentAsync(Scenario sc)
    {
        var resp = await ClientFor(sc.Customer).GetAsync($"/api/payment/intents/{sc.IntentId}");
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<PaymentIntentResponseDto>(TestHelper.JsonOptions))!;
    }

    private async Task<HttpResponseMessage> MarkReceivedAsync(Scenario sc)
    {
        var admin = await TestHelper.CreateAdminClientAsync(_factory);
        return await admin.PostAsJsonAsync($"/api/payment/intents/{sc.IntentId}/mark-received",
            new MarkPaymentReceivedDto { Note = "Cash collected" }, TestHelper.JsonOptions);
    }

    private Task<HttpResponseMessage> CancelAsCustomerAsync(Scenario sc) =>
        ClientFor(sc.Customer).PostAsJsonAsync($"/api/order/{sc.OrderId}/cancel",
            new CancelOrderDto { Reason = CancellationReason.CustomerRequest }, TestHelper.JsonOptions);

    private static ProcessGatewayPaymentDto GatewayPayment(int orderId) => new()
    {
        OrderId = orderId,
        Card = new GatewayCardDto
        {
            CardHolderName = "Test Customer",
            CardNumber = "5528790000000008",
            ExpireMonth = "12",
            ExpireYear = "2030",
            Cvc = "123"
        },
        Use3DSecure = false
    };

    [Fact]
    public async Task CodOrder_DeliveredThenMarkReceived_StaysDelivered()
    {
        var sc = await CreateScenarioAsync();
        var seller = ClientFor(sc.Seller);
        (await seller.PostAsJsonAsync($"/api/seller/orders/{sc.SellerOrderId}/ship",
            new { carrier = "Yurtici", trackingNumber = "TR123" }, TestHelper.JsonOptions))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await seller.PatchAsync($"/api/shipments/seller-orders/{sc.SellerOrderId}/deliver", null))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await GetOrderAsync(sc)).Status.Should().Be(OrderStatus.Delivered);

        var markReceived = await MarkReceivedAsync(sc);

        markReceived.StatusCode.Should().Be(HttpStatusCode.OK);
        var order = await GetOrderAsync(sc);
        order.Status.Should().Be(OrderStatus.Delivered);
        order.PaidAt.Should().NotBeNull();
        (await CancelAsCustomerAsync(sc)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await GetOrderAsync(sc)).Status.Should().Be(OrderStatus.Delivered);
    }

    [Fact]
    public async Task CodOrder_ConfirmedThenMarkReceived_StaysConfirmed()
    {
        var sc = await CreateScenarioAsync();
        (await ClientFor(sc.Seller).PostAsync($"/api/seller/orders/{sc.SellerOrderId}/confirm", null))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await GetOrderAsync(sc)).Status.Should().Be(OrderStatus.Confirmed);

        var markReceived = await MarkReceivedAsync(sc);

        markReceived.StatusCode.Should().Be(HttpStatusCode.OK);
        var order = await GetOrderAsync(sc);
        order.Status.Should().Be(OrderStatus.Confirmed);
        order.PaidAt.Should().NotBeNull();
    }

    [Fact]
    public async Task CancelledOrder_MarkReceived_ReturnsBadRequest_AndStaysCancelled()
    {
        var sc = await CreateScenarioAsync();
        (await CancelAsCustomerAsync(sc)).StatusCode.Should().Be(HttpStatusCode.OK);

        var markReceived = await MarkReceivedAsync(sc);

        markReceived.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var order = await GetOrderAsync(sc);
        order.Status.Should().Be(OrderStatus.Cancelled);
        order.PaidAt.Should().BeNull();
        (await GetIntentAsync(sc)).Status.Should().Be(PaymentStatus.Created);
    }

    [Fact]
    public async Task CancelledOrder_3DSConfirm_ReturnsBadRequest_AndIntentUnchanged()
    {
        var sc = await CreateScenarioAsync(PaymentMethod.CreditCard, price: 600m, threeDSecureCard: true);
        (await GetIntentAsync(sc)).Status.Should().Be(PaymentStatus.Pending);
        (await CancelAsCustomerAsync(sc)).StatusCode.Should().Be(HttpStatusCode.OK);
        var admin = await TestHelper.CreateAdminClientAsync(_factory);

        var confirm = await admin.PostAsJsonAsync($"/api/payment/intents/{sc.IntentId}/3ds/confirm",
            new { authenticationResult = "success" }, TestHelper.JsonOptions);

        confirm.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await GetIntentAsync(sc)).Status.Should().Be(PaymentStatus.Pending);
        (await GetOrderAsync(sc)).Status.Should().Be(OrderStatus.Cancelled);
    }

    [Fact]
    public async Task PendingOrder_MarkReceived_MovesToProcessing()
    {
        var sc = await CreateScenarioAsync();

        (await MarkReceivedAsync(sc)).StatusCode.Should().Be(HttpStatusCode.OK);

        var order = await GetOrderAsync(sc);
        order.Status.Should().Be(OrderStatus.Processing);
        order.PaidAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ConfirmedOrder_SimulateFail_StaysConfirmed()
    {
        var sc = await CreateScenarioAsync();
        (await ClientFor(sc.Seller).PostAsync($"/api/seller/orders/{sc.SellerOrderId}/confirm", null))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        var admin = await TestHelper.CreateAdminClientAsync(_factory);

        var fail = await admin.PostAsJsonAsync($"/api/payment/intents/{sc.IntentId}/simulate-fail",
            new SimulatePaymentDto { Reason = "Declined" }, TestHelper.JsonOptions);

        fail.StatusCode.Should().Be(HttpStatusCode.OK);
        (await GetOrderAsync(sc)).Status.Should().Be(OrderStatus.Confirmed);
    }

    [Fact]
    public async Task PendingOrder_SimulateFail_MovesToFailed()
    {
        var sc = await CreateScenarioAsync();
        var admin = await TestHelper.CreateAdminClientAsync(_factory);

        var fail = await admin.PostAsJsonAsync($"/api/payment/intents/{sc.IntentId}/simulate-fail",
            new SimulatePaymentDto { Reason = "Declined" }, TestHelper.JsonOptions);

        fail.StatusCode.Should().Be(HttpStatusCode.OK);
        (await GetOrderAsync(sc)).Status.Should().Be(OrderStatus.Failed);
    }

    [Fact]
    public async Task CancelledOrder_GatewayProcess_ReturnsBadRequest_AndStaysCancelled()
    {
        var sc = await CreateScenarioAsync(PaymentMethod.CreditCard);
        (await CancelAsCustomerAsync(sc)).StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await ClientFor(sc.Customer).PostAsJsonAsync("/api/payment/gateway/process",
            GatewayPayment(sc.OrderId), TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var order = await GetOrderAsync(sc);
        order.Status.Should().Be(OrderStatus.Cancelled);
        order.PaidAt.Should().BeNull();
        (await GetIntentAsync(sc)).Status.Should().Be(PaymentStatus.Created);
    }

    [Fact]
    public async Task CapturedIntent_GatewayProcessAgain_ReturnsBadRequest()
    {
        var sc = await CreateScenarioAsync(PaymentMethod.CreditCard);
        var customer = ClientFor(sc.Customer);
        (await customer.PostAsJsonAsync("/api/payment/gateway/process", GatewayPayment(sc.OrderId), TestHelper.JsonOptions))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        var captured = await GetIntentAsync(sc);
        captured.Status.Should().Be(PaymentStatus.Captured);

        var again = await customer.PostAsJsonAsync("/api/payment/gateway/process", GatewayPayment(sc.OrderId), TestHelper.JsonOptions);

        again.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var after = await GetIntentAsync(sc);
        after.Status.Should().Be(PaymentStatus.Captured);
        after.ExternalReference.Should().Be(captured.ExternalReference);
    }

    [Fact]
    public async Task ShippedOrder_GatewayProcess_DoesNotRegressStatus()
    {
        var sc = await CreateScenarioAsync(PaymentMethod.CreditCard);
        (await ClientFor(sc.Seller).PostAsJsonAsync($"/api/seller/orders/{sc.SellerOrderId}/ship",
            new { carrier = "Yurtici", trackingNumber = "TR123" }, TestHelper.JsonOptions))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await ClientFor(sc.Customer).PostAsJsonAsync("/api/payment/gateway/process",
            GatewayPayment(sc.OrderId), TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var order = await GetOrderAsync(sc);
        order.Status.Should().Be(OrderStatus.Shipped);
        order.PaidAt.Should().NotBeNull();
    }
}
