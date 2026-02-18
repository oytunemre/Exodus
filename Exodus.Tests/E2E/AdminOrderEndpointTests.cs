using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Exodus.Controllers.Admin;
using Exodus.Models.Dto;
using Exodus.Models.Dto.CartDto;
using Exodus.Models.Dto.ListingDto;
using Exodus.Models.Dto.Payment;
using Exodus.Models.Dto.ProductDto;
using Exodus.Models.Enums;
using FluentAssertions;
using Xunit;

namespace Exodus.Tests.E2E;

public class AdminOrderEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AdminOrderEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<(HttpClient Client, int OrderId)> CreateOrderAsync(string suffix)
    {
        var client = _factory.CreateClient();

        var seller = await TestHelper.RegisterAndLoginAsSellerAsync(client, $"admord{suffix}");
        var prodDto = new AddProductDto
        {
            ProductName = $"Admin Order Test {suffix}",
            ProductDescription = "For admin order tests",
            Barcodes = new List<string> { Guid.NewGuid().ToString("N") }
        };
        var prodResp = await client.PostAsJsonAsync("/api/product", prodDto, TestHelper.JsonOptions);
        var prod = await prodResp.Content.ReadFromJsonAsync<ProductResponseDto>(TestHelper.JsonOptions);

        var listingDto = new AddListingDto
        {
            ProductId = prod!.Id,
            SellerId = seller.UserId,
            Price = 250.00m,
            Stock = 50,
            Condition = ListingCondition.New
        };
        var lstResp = await client.PostAsJsonAsync("/api/listings", listingDto, TestHelper.JsonOptions);
        var listing = await lstResp.Content.ReadFromJsonAsync<ListingResponseDto>(TestHelper.JsonOptions);

        var customer = await TestHelper.RegisterAndLoginAsCustomerAsync(client, $"admord{suffix}");
        await client.PostAsJsonAsync("/api/cart/add",
            new AddToCartDto { UserId = customer.UserId, ListingId = listing!.Id, Quantity = 1 },
            TestHelper.JsonOptions);

        var addressDto = new CreateAddressDto
        {
            Title = "Home",
            FullName = "Test",
            Phone = "+905551234567",
            City = "Istanbul",
            District = "Kadikoy",
            AddressLine = "Test Sok. No:1"
        };
        var addrResp = await client.PostAsJsonAsync("/api/address", addressDto, TestHelper.JsonOptions);
        var address = await addrResp.Content.ReadFromJsonAsync<AddressResponseDto>(TestHelper.JsonOptions);

        var orderResp = await client.PostAsJsonAsync("/api/order/checkout",
            new CreateOrderDto { ShippingAddressId = address!.Id }, TestHelper.JsonOptions);
        var order = await orderResp.Content.ReadFromJsonAsync<OrderDetailResponseDto>(TestHelper.JsonOptions);

        return (client, order!.Id);
    }

    // ==========================================
    // AUTHORIZATION
    // ==========================================

    [Fact]
    public async Task GetAllOrders_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/admin/orders");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAllOrders_AsCustomer_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsCustomerAsync(client, "aocust");

        var response = await client.GetAsync("/api/admin/orders");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAllOrders_AsSeller_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsSellerAsync(client, "aosell");

        var response = await client.GetAsync("/api/admin/orders");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ==========================================
    // GET ALL ORDERS
    // ==========================================

    [Fact]
    public async Task GetAllOrders_AsAdmin_ReturnsOk()
    {
        var (client, _) = await CreateOrderAsync("gao1");
        await TestHelper.RegisterAndLoginAsAdminAsync(client, "gao1");

        var response = await client.GetAsync("/api/admin/orders");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        json.RootElement.GetProperty("items").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetAllOrders_WithPagination_ReturnsPagedResults()
    {
        var (client, _) = await CreateOrderAsync("gao2");
        await TestHelper.RegisterAndLoginAsAdminAsync(client, "gao2");

        var response = await client.GetAsync("/api/admin/orders?page=1&pageSize=5");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        json.RootElement.GetProperty("page").GetInt32().Should().Be(1);
        json.RootElement.GetProperty("pageSize").GetInt32().Should().Be(5);
    }

    [Fact]
    public async Task GetAllOrders_WithStatusFilter_ReturnsFiltered()
    {
        var (client, _) = await CreateOrderAsync("gao3");
        await TestHelper.RegisterAndLoginAsAdminAsync(client, "gao3");

        var response = await client.GetAsync("/api/admin/orders?status=Pending");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ==========================================
    // GET ORDER BY ID
    // ==========================================

    [Fact]
    public async Task GetOrder_ExistingId_ReturnsDetailedOrder()
    {
        var (client, orderId) = await CreateOrderAsync("go1");
        await TestHelper.RegisterAndLoginAsAdminAsync(client, "go1");

        var response = await client.GetAsync($"/api/admin/orders/{orderId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("orderNumber");
        content.Should().Contain("buyer");
        content.Should().Contain("sellerOrders");
        content.Should().Contain("priceBreakdown");
        content.Should().Contain("commissionInfo");
    }

    [Fact]
    public async Task GetOrder_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/admin/orders/1");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ==========================================
    // UPDATE ORDER STATUS
    // ==========================================

    [Fact]
    public async Task UpdateStatus_AsAdmin_ReturnsOk()
    {
        var (client, orderId) = await CreateOrderAsync("us1");
        await TestHelper.RegisterAndLoginAsAdminAsync(client, "us1");

        var dto = new AdminUpdateOrderStatusDto
        {
            Status = OrderStatus.Processing,
            Note = "Admin approved"
        };

        var response = await client.PatchAsJsonAsync($"/api/admin/orders/{orderId}/status", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<OrderDetailResponseDto>(TestHelper.JsonOptions);
        result!.Status.Should().Be(OrderStatus.Processing);
    }

    [Fact]
    public async Task UpdateStatus_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var dto = new AdminUpdateOrderStatusDto
        {
            Status = OrderStatus.Processing
        };

        var response = await client.PatchAsJsonAsync("/api/admin/orders/1/status", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ==========================================
    // REFUNDS
    // ==========================================

    [Fact]
    public async Task GetRefunds_AsAdmin_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsAdminAsync(client, "ref1");

        var response = await client.GetAsync("/api/admin/orders/refunds");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("items");
        content.Should().Contain("totalCount");
    }

    [Fact]
    public async Task GetRefunds_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/admin/orders/refunds");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ==========================================
    // STATISTICS
    // ==========================================

    [Fact]
    public async Task GetStatistics_AsAdmin_ReturnsOk()
    {
        var (client, _) = await CreateOrderAsync("st1");
        await TestHelper.RegisterAndLoginAsAdminAsync(client, "st1");

        var response = await client.GetAsync("/api/admin/orders/statistics");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("totalOrders");
        content.Should().Contain("pendingOrders");
    }

    [Fact]
    public async Task GetStatistics_WithDateRange_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsAdminAsync(client, "st2");

        var from = DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-dd");
        var to = DateTime.UtcNow.ToString("yyyy-MM-dd");

        var response = await client.GetAsync($"/api/admin/orders/statistics?fromDate={from}&toDate={to}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetStatistics_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/admin/orders/statistics");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
