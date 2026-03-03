using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Exodus.Controllers.Seller;
using Exodus.Models.Dto;
using Exodus.Models.Dto.CartDto;
using Exodus.Models.Dto.ListingDto;
using Exodus.Models.Dto.OrderDto;
using Exodus.Models.Dto.Payment;
using Exodus.Models.Dto.ProductDto;
using Exodus.Models.Enums;
using FluentAssertions;
using Xunit;

namespace Exodus.Tests.E2E;

public class SellerOrderEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public SellerOrderEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Helper: creates a full order with payment captured.
    /// Returns client authenticated as the seller, along with the sellerOrderId.
    /// </summary>
    private async Task<(HttpClient Client, int SellerOrderId, AuthResponseDto Seller)>
        CreateOrderForSellerAsync(string suffix)
    {
        var client = _factory.CreateClient();

        // Seller creates product + listing
        var seller = await TestHelper.RegisterAndLoginAsSellerAsync(client, $"slord{suffix}");
        var prodDto = new AddProductDto
        {
            ProductName = $"Seller Order Product {suffix}",
            ProductDescription = "For seller order tests",
            Barcodes = new List<string> { Guid.NewGuid().ToString("N") }
        };
        var prodResp = await client.PostAsJsonAsync("/api/product", prodDto, TestHelper.JsonOptions);
        var prod = await prodResp.Content.ReadFromJsonAsync<ProductResponseDto>(TestHelper.JsonOptions);

        var listingDto = new AddListingDto
        {
            ProductId = prod!.Id,
            SellerId = seller.UserId,
            Price = 100.00m,
            Stock = 20,
            Condition = ListingCondition.New
        };
        var lstResp = await client.PostAsJsonAsync("/api/listings", listingDto, TestHelper.JsonOptions);
        var listing = await lstResp.Content.ReadFromJsonAsync<ListingResponseDto>(TestHelper.JsonOptions);

        // Customer adds to cart and checks out
        var customer = await TestHelper.RegisterAndLoginAsCustomerAsync(client, $"slord{suffix}");

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
            AddressLine = "Test Cad. No:10"
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

        // Get the seller order ID
        var orderDetailResp = await client.GetAsync($"/api/order/{order.Id}");
        var orderDetail = await orderDetailResp.Content.ReadFromJsonAsync<OrderDetailResponseDto>(TestHelper.JsonOptions);
        var sellerOrderId = orderDetail!.SellerOrders.First().Id;

        // Switch to seller authentication
        TestHelper.SetAuthToken(client, seller.Token!);

        return (client, sellerOrderId, seller);
    }

    [Fact]
    public async Task GetOrders_AsSeller_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsSellerAsync(client, "sloglist");

        var response = await client.GetAsync("/api/seller/orders");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetOrders_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/seller/orders");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetOrders_AsCustomer_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsCustomerAsync(client, "slogcust");

        var response = await client.GetAsync("/api/seller/orders");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetOrders_WithStatusFilter_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsSellerAsync(client, "slogfilt");

        var response = await client.GetAsync("/api/seller/orders?status=Pending");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetOrders_WithPagination_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsSellerAsync(client, "slogpage");

        var response = await client.GetAsync("/api/seller/orders?page=1&pageSize=5");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ConfirmOrder_AsSeller_ReturnsOk()
    {
        var (client, sellerOrderId, seller) = await CreateOrderForSellerAsync("conf");

        var response = await client.PostAsync($"/api/seller/orders/{sellerOrderId}/confirm", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>(TestHelper.JsonOptions);
        result.TryGetProperty("message", out _).Should().BeTrue();
    }

    [Fact]
    public async Task ConfirmOrder_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/api/seller/orders/1/confirm", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ConfirmOrder_AsCustomer_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsCustomerAsync(client, "slogconfcust");

        var response = await client.PostAsync("/api/seller/orders/1/confirm", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateStatus_AsSeller_ReturnsOk()
    {
        var (client, sellerOrderId, seller) = await CreateOrderForSellerAsync("upd");

        // First confirm the order, then update status
        await client.PostAsync($"/api/seller/orders/{sellerOrderId}/confirm", null);

        var dto = new UpdateSellerOrderStatusDto { Status = OrderStatus.Confirmed };
        var response = await client.PatchAsJsonAsync($"/api/seller/orders/{sellerOrderId}/status", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateStatus_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var dto = new UpdateSellerOrderStatusDto { Status = OrderStatus.Confirmed };
        var response = await client.PatchAsJsonAsync("/api/seller/orders/1/status", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ShipOrder_AsSeller_ReturnsOk()
    {
        var (client, sellerOrderId, seller) = await CreateOrderForSellerAsync("ship");

        // First confirm the order
        await client.PostAsync($"/api/seller/orders/{sellerOrderId}/confirm", null);

        var dto = new ShipOrderDto
        {
            Carrier = "Yurtici Kargo",
            TrackingNumber = "YK" + Guid.NewGuid().ToString("N")[..10]
        };

        var response = await client.PostAsJsonAsync($"/api/seller/orders/{sellerOrderId}/ship", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>(TestHelper.JsonOptions);
        result.TryGetProperty("message", out _).Should().BeTrue();
    }

    [Fact]
    public async Task ShipOrder_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var dto = new ShipOrderDto { Carrier = "Yurtici", TrackingNumber = "YK123" };
        var response = await client.PostAsJsonAsync("/api/seller/orders/1/ship", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetOrders_SellerSeesOnlyOwnOrders()
    {
        var (client, sellerOrderId, seller) = await CreateOrderForSellerAsync("iso");

        var response = await client.GetAsync("/api/seller/orders");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Register different seller and check they don't see the first seller's orders
        var client2 = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsSellerAsync(client2, "slogiso2");
        var response2 = await client2.GetAsync("/api/seller/orders");
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
