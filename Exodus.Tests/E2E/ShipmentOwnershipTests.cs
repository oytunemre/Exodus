using System.Net;
using System.Net.Http.Json;
using Exodus.Data;
using Exodus.Models.Dto;
using Exodus.Models.Dto.CartDto;
using Exodus.Models.Dto.ListingDto;
using Exodus.Models.Dto.Payment;
using Exodus.Models.Dto.ProductDto;
using Exodus.Models.Dto.Shipment;
using Exodus.Models.Enums;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
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
        int OrderId, int SellerOrderA, int SellerOrderB, int ListingA, int ListingB);

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
        return new Scenario(sellerA, sellerB, customer, order.Id, soA, soB, listingA, listingB);
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

    [Theory]
    [InlineData(OrderStatus.Shipped)]
    [InlineData(OrderStatus.Delivered)]
    [InlineData(OrderStatus.Completed)]
    public async Task SellerStatusEndpoint_ShippedOrDelivered_ReturnsBadRequest(OrderStatus status)
    {
        var sc = await CreateScenarioAsync();

        var response = await ClientFor(sc.SellerA).PatchAsJsonAsync($"/api/seller/orders/{sc.SellerOrderA}/status",
            new { status = (int)status }, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var detail = await GetOrderAsync(sc.Customer, sc.OrderId);
        detail.SellerOrders.Single(x => x.Id == sc.SellerOrderA).Status
            .Should().NotBe(OrderStatus.Shipped).And.NotBe(OrderStatus.Delivered);
    }

    [Fact]
    public async Task SellerStatusEndpoint_Confirmed_ReturnsOk()
    {
        var sc = await CreateScenarioAsync();

        var response = await ClientFor(sc.SellerA).PatchAsJsonAsync($"/api/seller/orders/{sc.SellerOrderA}/status",
            new { status = (int)OrderStatus.Confirmed }, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ShipOrder_AfterShipAndDeliver_SellerOrderStaysDelivered()
    {
        var sc = await CreateScenarioAsync();
        var seller = ClientFor(sc.SellerA);
        await seller.PatchAsJsonAsync($"/api/shipments/seller-orders/{sc.SellerOrderA}/ship", Tracking, TestHelper.JsonOptions);
        await seller.PatchAsync($"/api/shipments/seller-orders/{sc.SellerOrderA}/deliver", null);

        var response = await seller.PatchAsJsonAsync($"/api/shipments/seller-orders/{sc.SellerOrderA}/ship", Tracking, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var detail = await GetOrderAsync(sc.Customer, sc.OrderId);
        detail.SellerOrders.Single(x => x.Id == sc.SellerOrderA).Status.Should().Be(OrderStatus.Delivered);
    }

    [Fact]
    public async Task Deliver_CancelledSellerOrderWithShippedShipment_ReturnsBadRequest_AndStaysCancelled()
    {
        var sc = await CreateScenarioAsync();
        (await ClientFor(sc.SellerA).PatchAsJsonAsync($"/api/shipments/seller-orders/{sc.SellerOrderA}/ship", Tracking, TestHelper.JsonOptions))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        // Customer cancel after shipping is refused by the API, so put the seller order into
        // "Cancelled with a Shipped shipment" directly to keep DeliverAsync's Cancelled guard covered.
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var sellerOrder = await db.SellerOrders.FindAsync(sc.SellerOrderA);
            sellerOrder!.Status = SellerOrderStatus.Cancelled;
            await db.SaveChangesAsync();
        }

        var response = await ClientFor(sc.SellerA).PatchAsync($"/api/shipments/seller-orders/{sc.SellerOrderA}/deliver", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var detail = await GetOrderAsync(sc.Customer, sc.OrderId);
        detail.SellerOrders.Single(x => x.Id == sc.SellerOrderA).Status.Should().Be(OrderStatus.Cancelled);
    }

    private Task<HttpResponseMessage> CancelAsCustomerAsync(Scenario sc) =>
        ClientFor(sc.Customer).PostAsJsonAsync($"/api/order/{sc.OrderId}/cancel",
            new CancelOrderDto { Reason = CancellationReason.CustomerRequest }, TestHelper.JsonOptions);

    private async Task<int> GetStockAsync(int listingId)
    {
        var resp = await _factory.CreateClient().GetAsync($"/api/listings/{listingId}");
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<ListingResponseDto>(TestHelper.JsonOptions))!.Stock;
    }

    private Task<HttpResponseMessage> SetStatusAsync(AuthResponseDto seller, int sellerOrderId, OrderStatus status) =>
        ClientFor(seller).PatchAsJsonAsync($"/api/seller/orders/{sellerOrderId}/status",
            new { status = status.ToString() }, TestHelper.JsonOptions);

    private async Task<OrderStatus> SellerOrderStatusAsync(Scenario sc, int sellerOrderId) =>
        (await GetOrderAsync(sc.Customer, sc.OrderId)).SellerOrders.Single(x => x.Id == sellerOrderId).Status;

    // ---- R12: customer cancel is refused once any seller order has shipped or been delivered

    [Fact]
    public async Task Cancel_AfterOneSellerDelivered_ReturnsBadRequest_AndSellerOrderStaysDelivered()
    {
        var sc = await CreateScenarioAsync();
        var seller = ClientFor(sc.SellerA);
        (await seller.PostAsJsonAsync($"/api/seller/orders/{sc.SellerOrderA}/ship",
            new { carrier = "Yurtici", trackingNumber = "TR123" }, TestHelper.JsonOptions)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await seller.PatchAsync($"/api/shipments/seller-orders/{sc.SellerOrderA}/deliver", null))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        var stockA = await GetStockAsync(sc.ListingA);
        var stockB = await GetStockAsync(sc.ListingB);

        var cancel = await CancelAsCustomerAsync(sc);

        cancel.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var detail = await GetOrderAsync(sc.Customer, sc.OrderId);
        detail.Status.Should().NotBe(OrderStatus.Cancelled);
        detail.SellerOrders.Single(x => x.Id == sc.SellerOrderA).Status.Should().Be(OrderStatus.Delivered);
        detail.SellerOrders.Single(x => x.Id == sc.SellerOrderB).Status.Should().NotBe(OrderStatus.Cancelled);
        (await GetStockAsync(sc.ListingA)).Should().Be(stockA);
        (await GetStockAsync(sc.ListingB)).Should().Be(stockB);
    }

    [Fact]
    public async Task Cancel_AfterOneSellerShipped_ReturnsBadRequest()
    {
        var sc = await CreateScenarioAsync();
        (await ClientFor(sc.SellerA).PostAsJsonAsync($"/api/seller/orders/{sc.SellerOrderA}/ship",
            new { carrier = "Yurtici", trackingNumber = "TR123" }, TestHelper.JsonOptions)).StatusCode.Should().Be(HttpStatusCode.OK);

        var cancel = await CancelAsCustomerAsync(sc);

        cancel.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await SellerOrderStatusAsync(sc, sc.SellerOrderA)).Should().Be(OrderStatus.Shipped);
        (await GetOrderAsync(sc.Customer, sc.OrderId)).Status.Should().NotBe(OrderStatus.Cancelled);
    }

    [Fact]
    public async Task Cancel_AfterSellerConfirmed_ReturnsOk_AndRestocks()
    {
        var sc = await CreateScenarioAsync();
        (await ClientFor(sc.SellerA).PostAsync($"/api/seller/orders/{sc.SellerOrderA}/confirm", null))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        var stockA = await GetStockAsync(sc.ListingA);

        var cancel = await CancelAsCustomerAsync(sc);

        cancel.StatusCode.Should().Be(HttpStatusCode.OK);
        (await GetOrderAsync(sc.Customer, sc.OrderId)).Status.Should().Be(OrderStatus.Cancelled);
        (await GetStockAsync(sc.ListingA)).Should().Be(stockA + 1);
    }

    // ---- /status is forward-only (Placed < Confirmed < Packed < Shipped < Delivered); Cancelled only before shipping

    [Theory]
    [InlineData(OrderStatus.Pending)]
    [InlineData(OrderStatus.Processing)]
    [InlineData(OrderStatus.Confirmed)]
    public async Task SellerStatusEndpoint_AfterShip_MovingBack_ReturnsBadRequest_AndStaysShipped(OrderStatus status)
    {
        var sc = await CreateScenarioAsync();
        (await ClientFor(sc.SellerA).PostAsJsonAsync($"/api/seller/orders/{sc.SellerOrderA}/ship",
            new { carrier = "Yurtici", trackingNumber = "TR123" }, TestHelper.JsonOptions)).StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await SetStatusAsync(sc.SellerA, sc.SellerOrderA, status);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await SellerOrderStatusAsync(sc, sc.SellerOrderA)).Should().Be(OrderStatus.Shipped);
    }

    [Fact]
    public async Task SellerStatusEndpoint_CancelAfterShip_ReturnsBadRequest_AndStaysShipped()
    {
        var sc = await CreateScenarioAsync();
        (await ClientFor(sc.SellerA).PostAsJsonAsync($"/api/seller/orders/{sc.SellerOrderA}/ship",
            new { carrier = "Yurtici", trackingNumber = "TR123" }, TestHelper.JsonOptions)).StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await SetStatusAsync(sc.SellerA, sc.SellerOrderA, OrderStatus.Cancelled);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await SellerOrderStatusAsync(sc, sc.SellerOrderA)).Should().Be(OrderStatus.Shipped);
    }

    [Fact]
    public async Task SellerStatusEndpoint_ConfirmedThenPending_ReturnsBadRequest()
    {
        var sc = await CreateScenarioAsync();
        (await SetStatusAsync(sc.SellerA, sc.SellerOrderA, OrderStatus.Confirmed)).StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await SetStatusAsync(sc.SellerA, sc.SellerOrderA, OrderStatus.Pending);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await SellerOrderStatusAsync(sc, sc.SellerOrderA)).Should().Be(OrderStatus.Confirmed);
    }

    [Fact]
    public async Task SellerStatusEndpoint_ConfirmTwice_IsIdempotent()
    {
        var sc = await CreateScenarioAsync();
        var seller = ClientFor(sc.SellerA);
        (await seller.PostAsync($"/api/seller/orders/{sc.SellerOrderA}/confirm", null)).StatusCode.Should().Be(HttpStatusCode.OK);

        (await seller.PostAsync($"/api/seller/orders/{sc.SellerOrderA}/confirm", null)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await SetStatusAsync(sc.SellerA, sc.SellerOrderA, OrderStatus.Confirmed)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await SellerOrderStatusAsync(sc, sc.SellerOrderA)).Should().Be(OrderStatus.Confirmed);
    }

    [Fact]
    public async Task SellerStatusEndpoint_ForwardMoves_AndCancelFromPacked_ReturnOk()
    {
        var sc = await CreateScenarioAsync();

        (await SetStatusAsync(sc.SellerA, sc.SellerOrderA, OrderStatus.Confirmed)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await SetStatusAsync(sc.SellerA, sc.SellerOrderA, OrderStatus.Processing)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await SellerOrderStatusAsync(sc, sc.SellerOrderA)).Should().Be(OrderStatus.Processing); // Packed
        (await SetStatusAsync(sc.SellerA, sc.SellerOrderA, OrderStatus.Cancelled)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await SellerOrderStatusAsync(sc, sc.SellerOrderA)).Should().Be(OrderStatus.Cancelled);
    }

    // ---- Parent order timestamps follow the derived status

    [Fact]
    public async Task ParentOrder_ShipAndDeliver_SetsShippedAtAndDeliveredAt()
    {
        var sc = await CreateScenarioAsync();
        foreach (var (seller, so) in new[] { (sc.SellerA, sc.SellerOrderA), (sc.SellerB, sc.SellerOrderB) })
            (await ClientFor(seller).PostAsJsonAsync($"/api/seller/orders/{so}/ship",
                new { carrier = "Yurtici", trackingNumber = "TR123" }, TestHelper.JsonOptions)).StatusCode.Should().Be(HttpStatusCode.OK);

        var shipped = await GetOrderAsync(sc.Customer, sc.OrderId);
        shipped.Status.Should().Be(OrderStatus.Shipped);
        shipped.ShippedAt.Should().NotBeNull();
        shipped.DeliveredAt.Should().BeNull();

        foreach (var (seller, so) in new[] { (sc.SellerA, sc.SellerOrderA), (sc.SellerB, sc.SellerOrderB) })
            (await ClientFor(seller).PatchAsync($"/api/shipments/seller-orders/{so}/deliver", null)).StatusCode.Should().Be(HttpStatusCode.OK);

        var delivered = await GetOrderAsync(sc.Customer, sc.OrderId);
        delivered.Status.Should().Be(OrderStatus.Delivered);
        delivered.DeliveredAt.Should().NotBeNull();
        delivered.ShippedAt.Should().Be(shipped.ShippedAt);
    }

    [Fact]
    public async Task ParentOrder_JumpsToDelivered_SetsShippedAtToo()
    {
        var sc = await CreateScenarioAsync();
        var sellerA = ClientFor(sc.SellerA);
        (await sellerA.PostAsJsonAsync($"/api/seller/orders/{sc.SellerOrderA}/ship",
            new { carrier = "Yurtici", trackingNumber = "TR123" }, TestHelper.JsonOptions)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await sellerA.PatchAsync($"/api/shipments/seller-orders/{sc.SellerOrderA}/deliver", null)).StatusCode.Should().Be(HttpStatusCode.OK);

        // Seller B cancels its untouched part: the only active seller order is Delivered.
        (await SetStatusAsync(sc.SellerB, sc.SellerOrderB, OrderStatus.Cancelled)).StatusCode.Should().Be(HttpStatusCode.OK);

        var detail = await GetOrderAsync(sc.Customer, sc.OrderId);
        detail.Status.Should().Be(OrderStatus.Delivered);
        detail.ShippedAt.Should().NotBeNull();
        detail.DeliveredAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ShipAfterCustomerCancel_ReturnsBadRequest()
    {
        var sc = await CreateScenarioAsync();
        await ClientFor(sc.Customer).PostAsJsonAsync($"/api/order/{sc.OrderId}/cancel",
            new CancelOrderDto { Reason = CancellationReason.CustomerRequest }, TestHelper.JsonOptions);

        var response = await ClientFor(sc.SellerA).PatchAsJsonAsync($"/api/shipments/seller-orders/{sc.SellerOrderA}/ship", Tracking, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
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
