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

public class ShipmentEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ShipmentEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Helper: creates a full order with payment captured, returns (client, orderId, sellerOrderId, seller, customer).
    /// Client is authenticated as the seller after this call.
    /// </summary>
    private async Task<(HttpClient Client, int OrderId, int SellerOrderId, AuthResponseDto Seller, AuthResponseDto Customer)>
        CreatePaidOrderAsync(string suffix)
    {
        var client = _factory.CreateClient();

        // Seller creates product + listing
        var seller = await TestHelper.RegisterAndLoginAsSellerAsync(client, $"ship{suffix}");
        var prodDto = new AddProductDto
        {
            ProductName = $"Ship Test Product {suffix}",
            ProductDescription = "For shipment tests",
            Barcodes = new List<string> { Guid.NewGuid().ToString("N") }
        };
        var prodResp = await client.PostAsJsonAsync("/api/product", prodDto, TestHelper.JsonOptions);
        var prod = await prodResp.Content.ReadFromJsonAsync<ProductResponseDto>(TestHelper.JsonOptions);

        var listingDto = new AddListingDto
        {
            ProductId = prod!.Id,
            SellerId = seller.UserId,
            Price = 150.00m,
            Stock = 50,
            Condition = ListingCondition.New
        };
        var lstResp = await client.PostAsJsonAsync("/api/listings", listingDto, TestHelper.JsonOptions);
        var listing = await lstResp.Content.ReadFromJsonAsync<ListingResponseDto>(TestHelper.JsonOptions);

        // Customer adds to cart and checks out
        var customer = await TestHelper.RegisterAndLoginAsCustomerAsync(client, $"ship{suffix}");

        await client.PostAsJsonAsync("/api/cart/add",
            new AddToCartDto { UserId = customer.UserId, ListingId = listing!.Id, Quantity = 1 },
            TestHelper.JsonOptions);

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

        var orderDto = new CreateOrderDto { ShippingAddressId = address!.Id };
        var orderResp = await client.PostAsJsonAsync("/api/order/checkout", orderDto, TestHelper.JsonOptions);
        var order = await orderResp.Content.ReadFromJsonAsync<OrderDetailResponseDto>(TestHelper.JsonOptions);

        // Create and capture payment
        var paymentDto = new CreatePaymentIntentDto
        {
            OrderId = order!.Id,
            Method = PaymentMethod.CreditCard,
            Currency = "TRY"
        };
        var payResp = await client.PostAsJsonAsync("/api/payment/intents", paymentDto, TestHelper.JsonOptions);
        var payment = await payResp.Content.ReadFromJsonAsync<PaymentIntentResponseDto>(TestHelper.JsonOptions);

        await client.PostAsJsonAsync($"/api/payment/intents/{payment!.Id}/simulate-success",
            new SimulatePaymentDto { Reason = "Test" }, TestHelper.JsonOptions);

        // Get seller order ID
        var orderDetailResp = await client.GetAsync($"/api/order/{order.Id}");
        var orderDetail = await orderDetailResp.Content.ReadFromJsonAsync<OrderDetailResponseDto>(TestHelper.JsonOptions);
        var sellerOrderId = orderDetail!.SellerOrders.First().Id;

        // Switch to seller
        TestHelper.SetAuthToken(client, seller.Token!);

        return (client, order.Id, sellerOrderId, seller, customer);
    }

    // ==========================================
    // AUTHORIZATION
    // ==========================================

    [Fact]
    public async Task GetBySellerOrderId_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/shipments/seller-orders/1");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Ship_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var dto = new ShipSellerOrderDto
        {
            Carrier = "Yurtici",
            TrackingNumber = "YK123456789"
        };

        var response = await client.PatchAsJsonAsync("/api/shipments/seller-orders/1/ship", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Ship_AsCustomer_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsCustomerAsync(client, "shipcust");

        var dto = new ShipSellerOrderDto
        {
            Carrier = "Yurtici",
            TrackingNumber = "YK123456789"
        };

        var response = await client.PatchAsJsonAsync("/api/shipments/seller-orders/1/ship", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ==========================================
    // SHIP
    // ==========================================

    [Fact]
    public async Task Ship_ValidSellerOrder_ReturnsOk()
    {
        var (client, _, sellerOrderId, _, _) = await CreatePaidOrderAsync("s1");

        var dto = new ShipSellerOrderDto
        {
            Carrier = "Yurtici",
            TrackingNumber = "YK" + Guid.NewGuid().ToString("N")[..10]
        };

        var response = await client.PatchAsJsonAsync($"/api/shipments/seller-orders/{sellerOrderId}/ship", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ShipmentDto>(TestHelper.JsonOptions);
        result.Should().NotBeNull();
        result!.Carrier.Should().Be("Yurtici");
        result.Status.Should().Be(ShipmentStatus.Shipped);
        result.ShippedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Ship_NonExistentSellerOrder_ReturnsNotFound()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsSellerAsync(client, "shipnf");

        var dto = new ShipSellerOrderDto
        {
            Carrier = "Yurtici",
            TrackingNumber = "YK999999999"
        };

        var response = await client.PatchAsJsonAsync("/api/shipments/seller-orders/99999/ship", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ==========================================
    // DELIVER
    // ==========================================

    [Fact]
    public async Task Deliver_ShippedOrder_ReturnsOk()
    {
        var (client, _, sellerOrderId, _, _) = await CreatePaidOrderAsync("d1");

        // Ship first
        var shipDto = new ShipSellerOrderDto
        {
            Carrier = "Yurtici",
            TrackingNumber = "YK" + Guid.NewGuid().ToString("N")[..10]
        };
        await client.PatchAsJsonAsync($"/api/shipments/seller-orders/{sellerOrderId}/ship", shipDto, TestHelper.JsonOptions);

        // Deliver
        var response = await client.PatchAsync($"/api/shipments/seller-orders/{sellerOrderId}/deliver", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ShipmentDto>(TestHelper.JsonOptions);
        result!.Status.Should().Be(ShipmentStatus.Delivered);
        result.DeliveredAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Deliver_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PatchAsync("/api/shipments/seller-orders/1/deliver", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ==========================================
    // GET SHIPMENT
    // ==========================================

    [Fact]
    public async Task GetBySellerOrderId_AfterShipping_ReturnsShipment()
    {
        var (client, _, sellerOrderId, _, _) = await CreatePaidOrderAsync("g1");

        // Ship first
        var shipDto = new ShipSellerOrderDto
        {
            Carrier = "Aras",
            TrackingNumber = "AR" + Guid.NewGuid().ToString("N")[..10]
        };
        await client.PatchAsJsonAsync($"/api/shipments/seller-orders/{sellerOrderId}/ship", shipDto, TestHelper.JsonOptions);

        // Get shipment
        var response = await client.GetAsync($"/api/shipments/seller-orders/{sellerOrderId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ShipmentDto>(TestHelper.JsonOptions);
        result.Should().NotBeNull();
        result!.Carrier.Should().Be("Aras");
        result.SellerOrderId.Should().Be(sellerOrderId);
    }

    // ==========================================
    // TIMELINE
    // ==========================================

    [Fact]
    public async Task Timeline_AfterShipAndDeliver_ReturnsTimeline()
    {
        var (client, _, sellerOrderId, _, _) = await CreatePaidOrderAsync("tl1");

        // Ship
        var shipDto = new ShipSellerOrderDto
        {
            Carrier = "MNG",
            TrackingNumber = "MNG" + Guid.NewGuid().ToString("N")[..10]
        };
        await client.PatchAsJsonAsync($"/api/shipments/seller-orders/{sellerOrderId}/ship", shipDto, TestHelper.JsonOptions);

        // Deliver
        await client.PatchAsync($"/api/shipments/seller-orders/{sellerOrderId}/deliver", null);

        // Get timeline
        var response = await client.GetAsync($"/api/shipments/seller-orders/{sellerOrderId}/timeline");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Timeline_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/shipments/seller-orders/1/timeline");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ==========================================
    // FULL SHIPMENT LIFECYCLE
    // ==========================================

    [Fact]
    public async Task FullLifecycle_ShipThenDeliver_StatusesCorrect()
    {
        var (client, _, sellerOrderId, _, _) = await CreatePaidOrderAsync("fl1");

        // Ship
        var shipDto = new ShipSellerOrderDto
        {
            Carrier = "PTT",
            TrackingNumber = "PTT" + Guid.NewGuid().ToString("N")[..10]
        };
        var shipResp = await client.PatchAsJsonAsync($"/api/shipments/seller-orders/{sellerOrderId}/ship", shipDto, TestHelper.JsonOptions);
        shipResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var shipped = await shipResp.Content.ReadFromJsonAsync<ShipmentDto>(TestHelper.JsonOptions);
        shipped!.Status.Should().Be(ShipmentStatus.Shipped);

        // Deliver
        var deliverResp = await client.PatchAsync($"/api/shipments/seller-orders/{sellerOrderId}/deliver", null);
        deliverResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var delivered = await deliverResp.Content.ReadFromJsonAsync<ShipmentDto>(TestHelper.JsonOptions);
        delivered!.Status.Should().Be(ShipmentStatus.Delivered);
        delivered.DeliveredAt.Should().NotBeNull();
    }
}
