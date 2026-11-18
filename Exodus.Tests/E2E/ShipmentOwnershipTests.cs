using System.Net;
using System.Net.Http.Json;
using Exodus.Models.Dto;
using Exodus.Models.Dto.CartDto;
using Exodus.Models.Dto.ListingDto;
using Exodus.Models.Dto.Payment;
using Exodus.Models.Dto.ProductDto;
using Exodus.Models.Dto.Shipment;
using Exodus.Models.Enums;
using FluentAssertions;
using Xunit;

namespace Exodus.Tests.E2E;

public class ShipmentOwnershipTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ShipmentOwnershipTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private sealed record Scenario(
        AuthResponseDto SellerA, AuthResponseDto SellerB, AuthResponseDto Customer,
        int OrderId, int SellerOrderA, int SellerOrderB);

    private async Task<int> CreateListingAsync(AuthResponseDto seller, string name)
    {
        var client = _factory.CreateClient();
        TestHelper.SetAuthToken(client, seller.Token!);
        var prodResp = await client.PostAsJsonAsync("/api/product", new AddProductDto
        {
            ProductName = name,
            ProductDescription = "Ownership tests",
            Barcodes = new List<string> { Guid.NewGuid().ToString("N") }
        }, TestHelper.JsonOptions);
        var prod = await prodResp.Content.ReadFromJsonAsync<ProductResponseDto>(TestHelper.JsonOptions);

        var lstResp = await client.PostAsJsonAsync("/api/listings", new AddListingDto
        {
            ProductId = prod!.Id,
            SellerId = seller.UserId,
            Price = 100m,
            Stock = 50,
            Condition = ListingCondition.New
        }, TestHelper.JsonOptions);
        var listing = await lstResp.Content.ReadFromJsonAsync<ListingResponseDto>(TestHelper.JsonOptions);
        return listing!.Id;
    }

    /// <summary>Two sellers, one customer, one order with a seller order per seller. Payment captured when paid=true.</summary>
    private async Task<Scenario> CreateScenarioAsync(bool paid = true)
    {
        var s = Guid.NewGuid().ToString("N")[..8];
        var setup = _factory.CreateClient();
        var sellerA = await TestHelper.RegisterAndLoginAsSellerAsync(setup, $"own{s}a");
        var sellerB = await TestHelper.RegisterAndLoginAsSellerAsync(setup, $"own{s}b");
        var listingA = await CreateListingAsync(sellerA, $"Own A {s}");
        var listingB = await CreateListingAsync(sellerB, $"Own B {s}");

        var client = _factory.CreateClient();
        var customer = await TestHelper.RegisterAndLoginAsCustomerAsync(client, $"own{s}");
        foreach (var id in new[] { listingA, listingB })
            await client.PostAsJsonAsync("/api/cart/add",
                new AddToCartDto { UserId = customer.UserId, ListingId = id, Quantity = 1 }, TestHelper.JsonOptions);

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
        var order = await orderResp.Content.ReadFromJsonAsync<OrderDetailResponseDto>(TestHelper.JsonOptions);

        if (paid)
        {
            var payResp = await client.PostAsJsonAsync("/api/payment/intents", new CreatePaymentIntentDto
            {
                OrderId = order!.Id,
                Method = PaymentMethod.CreditCard,
                Currency = "TRY"
            }, TestHelper.JsonOptions);
            var payment = await payResp.Content.ReadFromJsonAsync<PaymentIntentResponseDto>(TestHelper.JsonOptions);
            await TestHelper.SimulatePaymentSuccessAsAdminAsync(_factory, payment!.Id);
        }

        var detail = await GetOrderAsync(customer, order!.Id);
        var soA = detail.SellerOrders.Single(x => x.SellerId == sellerA.UserId).Id;
        var soB = detail.SellerOrders.Single(x => x.SellerId == sellerB.UserId).Id;
        return new Scenario(sellerA, sellerB, customer, order.Id, soA, soB);
    }

    private HttpClient ClientFor(AuthResponseDto user)
    {
        var client = _factory.CreateClient();
        TestHelper.SetAuthToken(client, user.Token!);
        return client;
    }

    private async Task<OrderDetailResponseDto> GetOrderAsync(AuthResponseDto customer, int orderId)
    {
        var resp = await ClientFor(customer).GetAsync($"/api/order/{orderId}");
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<OrderDetailResponseDto>(TestHelper.JsonOptions))!;
    }

    private static readonly ShipSellerOrderDto Tracking = new() { Carrier = "Yurtici", TrackingNumber = "TR123" };

    [Theory]
    [InlineData("")]
    [InlineData("/timeline")]
    public async Task GetShipment_AsOtherSeller_ReturnsForbidden(string suffix)
    {
        var sc = await CreateScenarioAsync();
        (await ClientFor(sc.SellerA).PatchAsJsonAsync($"/api/shipments/seller-orders/{sc.SellerOrderA}/ship", Tracking, TestHelper.JsonOptions))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await ClientFor(sc.SellerB).GetAsync($"/api/shipments/seller-orders/{sc.SellerOrderA}{suffix}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ShipViaShipments_AsOtherSeller_ReturnsForbidden()
    {
        var sc = await CreateScenarioAsync();

        var response = await ClientFor(sc.SellerB)
            .PatchAsJsonAsync($"/api/shipments/seller-orders/{sc.SellerOrderA}/ship", Tracking, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeliverViaShipments_AsOtherSeller_ReturnsForbidden()
    {
        var sc = await CreateScenarioAsync();
        await ClientFor(sc.SellerA).PatchAsJsonAsync($"/api/shipments/seller-orders/{sc.SellerOrderA}/ship", Tracking, TestHelper.JsonOptions);

        var response = await ClientFor(sc.SellerB).PatchAsync($"/api/shipments/seller-orders/{sc.SellerOrderA}/deliver", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetShipment_AsAdmin_ReturnsOk()
    {
        var sc = await CreateScenarioAsync();
        await ClientFor(sc.SellerA).PatchAsJsonAsync($"/api/shipments/seller-orders/{sc.SellerOrderA}/ship", Tracking, TestHelper.JsonOptions);
        var admin = await TestHelper.CreateAdminClientAsync(_factory);

        var response = await admin.GetAsync($"/api/shipments/seller-orders/{sc.SellerOrderA}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task LegacySellerOrdersRoute_IsGone()
    {
        var sc = await CreateScenarioAsync();

        var response = await ClientFor(sc.SellerA).GetAsync($"/api/seller/{sc.SellerA.UserId}/orders");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ShipOrder_WithTracking_CreatesShipment()
    {
        var sc = await CreateScenarioAsync();
        var seller = ClientFor(sc.SellerA);

        var ship = await seller.PostAsJsonAsync($"/api/seller/orders/{sc.SellerOrderA}/ship",
            new { carrier = "Yurtici", trackingNumber = "TR123" }, TestHelper.JsonOptions);
        ship.StatusCode.Should().Be(HttpStatusCode.OK);

        var get = await seller.GetAsync($"/api/shipments/seller-orders/{sc.SellerOrderA}");
        get.StatusCode.Should().Be(HttpStatusCode.OK);
        var shipment = await get.Content.ReadFromJsonAsync<ShipmentDto>(TestHelper.JsonOptions);
        shipment!.TrackingNumber.Should().Be("TR123");
        shipment.Status.Should().Be(ShipmentStatus.Shipped);

        var deliver = await seller.PatchAsync($"/api/shipments/seller-orders/{sc.SellerOrderA}/deliver", null);
        deliver.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("Yurtici", null)]
    [InlineData(null, "TR123")]
    [InlineData("  ", "TR123")]
    public async Task ShipOrder_WithoutTracking_ReturnsBadRequest(string? carrier, string? tracking)
    {
        var sc = await CreateScenarioAsync();

        var response = await ClientFor(sc.SellerA).PostAsJsonAsync($"/api/seller/orders/{sc.SellerOrderA}/ship",
            new { carrier, trackingNumber = tracking }, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShipOrder_AfterDelivered_ReturnsBadRequest()
    {
        var sc = await CreateScenarioAsync();
        var seller = ClientFor(sc.SellerA);
        await seller.PostAsJsonAsync($"/api/seller/orders/{sc.SellerOrderA}/ship",
            new { carrier = "Yurtici", trackingNumber = "TR123" }, TestHelper.JsonOptions);
        await seller.PatchAsync($"/api/shipments/seller-orders/{sc.SellerOrderA}/deliver", null);

        var response = await seller.PostAsJsonAsync($"/api/seller/orders/{sc.SellerOrderA}/ship",
            new { carrier = "Yurtici", trackingNumber = "TR999" }, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShipOrder_AsOtherSeller_ReturnsForbidden()
    {
        var sc = await CreateScenarioAsync();

        var response = await ClientFor(sc.SellerB).PostAsJsonAsync($"/api/seller/orders/{sc.SellerOrderA}/ship",
            new { carrier = "Yurtici", trackingNumber = "TR123" }, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ParentOrderStatus_FollowsShipmentsPath()
    {
        var sc = await CreateScenarioAsync();

        var first = await ClientFor(sc.SellerA)
            .PatchAsJsonAsync($"/api/shipments/seller-orders/{sc.SellerOrderA}/ship", Tracking, TestHelper.JsonOptions);
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        (await GetOrderAsync(sc.Customer, sc.OrderId)).Status.Should().NotBe(OrderStatus.Shipped);

        var second = await ClientFor(sc.SellerB)
            .PatchAsJsonAsync($"/api/shipments/seller-orders/{sc.SellerOrderB}/ship", Tracking, TestHelper.JsonOptions);
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        (await GetOrderAsync(sc.Customer, sc.OrderId)).Status.Should().Be(OrderStatus.Shipped);
    }

    [Fact]
    public async Task ParentOrderStatus_FollowsSellerOrdersShipPath()
    {
        var sc = await CreateScenarioAsync();
        var body = new { carrier = "Yurtici", trackingNumber = "TR123" };

        await ClientFor(sc.SellerA).PostAsJsonAsync($"/api/seller/orders/{sc.SellerOrderA}/ship", body, TestHelper.JsonOptions);
        (await GetOrderAsync(sc.Customer, sc.OrderId)).Status.Should().NotBe(OrderStatus.Shipped);

        await ClientFor(sc.SellerB).PostAsJsonAsync($"/api/seller/orders/{sc.SellerOrderB}/ship", body, TestHelper.JsonOptions);
        (await GetOrderAsync(sc.Customer, sc.OrderId)).Status.Should().Be(OrderStatus.Shipped);
    }

    [Fact]
    public async Task PaidOrder_OneSellerConfirms_DoesNotRegressToPending()
    {
        var sc = await CreateScenarioAsync();
        (await GetOrderAsync(sc.Customer, sc.OrderId)).Status.Should().Be(OrderStatus.Processing);

        var a = await ClientFor(sc.SellerA).PostAsync($"/api/seller/orders/{sc.SellerOrderA}/confirm", null);
        a.StatusCode.Should().Be(HttpStatusCode.OK);
        (await GetOrderAsync(sc.Customer, sc.OrderId)).Status.Should().Be(OrderStatus.Processing);

        var b = await ClientFor(sc.SellerB).PostAsync($"/api/seller/orders/{sc.SellerOrderB}/confirm", null);
        b.StatusCode.Should().Be(HttpStatusCode.OK);
        (await GetOrderAsync(sc.Customer, sc.OrderId)).Status.Should().Be(OrderStatus.Confirmed);
    }

    [Fact]
    public async Task CancelledOrder_SellerConfirm_ReturnsBadRequest_AndStaysCancelled()
    {
        var sc = await CreateScenarioAsync(paid: false);
        var cancel = await ClientFor(sc.Customer).PostAsJsonAsync($"/api/order/{sc.OrderId}/cancel",
            new CancelOrderDto { Reason = CancellationReason.CustomerRequest }, TestHelper.JsonOptions);
        cancel.StatusCode.Should().Be(HttpStatusCode.OK);

        var confirm = await ClientFor(sc.SellerA).PostAsync($"/api/seller/orders/{sc.SellerOrderA}/confirm", null);

        confirm.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await GetOrderAsync(sc.Customer, sc.OrderId)).Status.Should().Be(OrderStatus.Cancelled);
    }
}
