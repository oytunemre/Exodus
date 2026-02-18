using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Exodus.Controllers.Admin;
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

public class AdminShipmentEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AdminShipmentEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Creates a paid order, ships it, and returns (client, shipmentId, sellerOrderId).
    /// Client is authenticated as admin after this call.
    /// </summary>
    private async Task<(HttpClient Client, int ShipmentId, int SellerOrderId)> CreateShippedOrderAsync(string suffix)
    {
        var client = _factory.CreateClient();

        // Seller creates product + listing
        var seller = await TestHelper.RegisterAndLoginAsSellerAsync(client, $"admship{suffix}");
        var prodDto = new AddProductDto
        {
            ProductName = $"Admin Ship Test {suffix}",
            ProductDescription = "For admin shipment tests",
            Barcodes = new List<string> { Guid.NewGuid().ToString("N") }
        };
        var prodResp = await client.PostAsJsonAsync("/api/product", prodDto, TestHelper.JsonOptions);
        var prod = await prodResp.Content.ReadFromJsonAsync<ProductResponseDto>(TestHelper.JsonOptions);

        var listingDto = new AddListingDto
        {
            ProductId = prod!.Id,
            SellerId = seller.UserId,
            Price = 300.00m,
            Stock = 50,
            Condition = ListingCondition.New
        };
        var lstResp = await client.PostAsJsonAsync("/api/listings", listingDto, TestHelper.JsonOptions);
        var listing = await lstResp.Content.ReadFromJsonAsync<ListingResponseDto>(TestHelper.JsonOptions);

        // Customer creates order
        var customer = await TestHelper.RegisterAndLoginAsCustomerAsync(client, $"admship{suffix}");
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

        // Pay for the order
        var payDto = new CreatePaymentIntentDto
        {
            OrderId = order!.Id,
            Method = PaymentMethod.CreditCard,
            Currency = "TRY"
        };
        var payResp = await client.PostAsJsonAsync("/api/payment/intents", payDto, TestHelper.JsonOptions);
        var payment = await payResp.Content.ReadFromJsonAsync<PaymentIntentResponseDto>(TestHelper.JsonOptions);
        await client.PostAsJsonAsync($"/api/payment/intents/{payment!.Id}/simulate-success",
            new SimulatePaymentDto { Reason = "Test" }, TestHelper.JsonOptions);

        // Get seller order ID
        var orderDetailResp = await client.GetAsync($"/api/order/{order.Id}");
        var orderDetail = await orderDetailResp.Content.ReadFromJsonAsync<OrderDetailResponseDto>(TestHelper.JsonOptions);
        var sellerOrderId = orderDetail!.SellerOrders.First().Id;

        // Ship as seller
        TestHelper.SetAuthToken(client, seller.Token!);
        var shipDto = new ShipSellerOrderDto
        {
            Carrier = "Yurtici",
            TrackingNumber = "YK" + Guid.NewGuid().ToString("N")[..10]
        };
        var shipResp = await client.PatchAsJsonAsync($"/api/shipments/seller-orders/{sellerOrderId}/ship", shipDto, TestHelper.JsonOptions);
        var shipment = await shipResp.Content.ReadFromJsonAsync<ShipmentDto>(TestHelper.JsonOptions);

        // Switch to admin
        await TestHelper.RegisterAndLoginAsAdminAsync(client, $"admship{suffix}");

        return (client, shipment!.Id, sellerOrderId);
    }

    // ==========================================
    // AUTHORIZATION
    // ==========================================

    [Fact]
    public async Task GetShipments_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/admin/shipments");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetShipments_AsCustomer_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsCustomerAsync(client, "ascust");

        var response = await client.GetAsync("/api/admin/shipments");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ==========================================
    // GET ALL SHIPMENTS
    // ==========================================

    [Fact]
    public async Task GetShipments_AsAdmin_ReturnsOk()
    {
        var (client, _, _) = await CreateShippedOrderAsync("gs1");

        var response = await client.GetAsync("/api/admin/shipments");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        json.RootElement.GetProperty("items").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetShipments_WithPagination_ReturnsPagedResults()
    {
        var (client, _, _) = await CreateShippedOrderAsync("gs2");

        var response = await client.GetAsync("/api/admin/shipments?page=1&pageSize=5");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        json.RootElement.GetProperty("page").GetInt32().Should().Be(1);
        json.RootElement.GetProperty("pageSize").GetInt32().Should().Be(5);
    }

    [Fact]
    public async Task GetShipments_WithStatusFilter_ReturnsFiltered()
    {
        var (client, _, _) = await CreateShippedOrderAsync("gs3");

        var response = await client.GetAsync("/api/admin/shipments?status=Shipped");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetShipments_WithCarrierFilter_ReturnsFiltered()
    {
        var (client, _, _) = await CreateShippedOrderAsync("gs4");

        var response = await client.GetAsync("/api/admin/shipments?carrier=Yurtici");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ==========================================
    // GET SHIPMENT DETAILS
    // ==========================================

    [Fact]
    public async Task GetShipment_ExistingId_ReturnsOk()
    {
        var (client, shipmentId, _) = await CreateShippedOrderAsync("gsd1");

        var response = await client.GetAsync($"/api/admin/shipments/{shipmentId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("carrier");
        content.Should().Contain("trackingNumber");
        content.Should().Contain("sellerOrder");
        content.Should().Contain("seller");
        content.Should().Contain("buyer");
    }

    // ==========================================
    // UPDATE STATUS
    // ==========================================

    [Fact]
    public async Task UpdateStatus_ToDelivered_ReturnsOk()
    {
        var (client, shipmentId, _) = await CreateShippedOrderAsync("usq1");

        var dto = new UpdateShipmentStatusDto
        {
            Status = ShipmentStatus.Delivered,
            Description = "Delivered by courier",
            Location = "Istanbul"
        };

        var response = await client.PatchAsJsonAsync($"/api/admin/shipments/{shipmentId}/status", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Shipment status updated");
        content.Should().Contain("Delivered");
    }

    [Fact]
    public async Task UpdateStatus_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var dto = new UpdateShipmentStatusDto
        {
            Status = ShipmentStatus.Delivered
        };

        var response = await client.PatchAsJsonAsync("/api/admin/shipments/1/status", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ==========================================
    // UPDATE TRACKING
    // ==========================================

    [Fact]
    public async Task UpdateTracking_AsAdmin_ReturnsOk()
    {
        var (client, shipmentId, _) = await CreateShippedOrderAsync("ut1");

        var dto = new UpdateTrackingDto
        {
            TrackingNumber = "NEW-TRACK-" + Guid.NewGuid().ToString("N")[..8],
            Carrier = "Aras"
        };

        var response = await client.PatchAsJsonAsync($"/api/admin/shipments/{shipmentId}/tracking", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Tracking information updated");
        content.Should().Contain("Aras");
    }

    // ==========================================
    // STATISTICS
    // ==========================================

    [Fact]
    public async Task GetStatistics_AsAdmin_ReturnsOk()
    {
        var (client, _, _) = await CreateShippedOrderAsync("ss1");

        var response = await client.GetAsync("/api/admin/shipments/statistics");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("totalShipments");
        content.Should().Contain("currentStatus");
    }

    [Fact]
    public async Task GetStatistics_WithDateRange_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsAdminAsync(client, "ss2");

        var from = DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-dd");
        var to = DateTime.UtcNow.ToString("yyyy-MM-dd");

        var response = await client.GetAsync($"/api/admin/shipments/statistics?fromDate={from}&toDate={to}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetStatistics_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/admin/shipments/statistics");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ==========================================
    // CARRIER MANAGEMENT
    // ==========================================

    [Fact]
    public async Task CreateCarrier_AsAdmin_ReturnsCreated()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsAdminAsync(client, "cc1");

        var dto = new CreateCarrierDto
        {
            Name = "Test Carrier",
            Code = "TC" + Guid.NewGuid().ToString("N")[..4].ToUpper(),
            Website = "https://testcarrier.com",
            Phone = "+901234567890",
            IsActive = true,
            DefaultRate = 15.00m,
            FreeShippingThreshold = 200.00m,
            TrackingUrlTemplate = "https://testcarrier.com/track/{tracking}"
        };

        var response = await client.PostAsJsonAsync("/api/admin/shipments/carriers", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Carrier created successfully");
    }

    [Fact]
    public async Task GetCarriers_AsAdmin_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsAdminAsync(client, "gc1");

        var response = await client.GetAsync("/api/admin/shipments/carriers");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetCarrier_ExistingId_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsAdminAsync(client, "gci1");

        // Create carrier first
        var createDto = new CreateCarrierDto
        {
            Name = "Get Test Carrier",
            Code = "GTC" + Guid.NewGuid().ToString("N")[..3].ToUpper(),
            IsActive = true
        };
        var createResp = await client.PostAsJsonAsync("/api/admin/shipments/carriers", createDto, TestHelper.JsonOptions);
        var content = await createResp.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        var carrierId = json.RootElement.GetProperty("carrierId").GetInt32();

        var response = await client.GetAsync($"/api/admin/shipments/carriers/{carrierId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateCarrier_AsAdmin_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsAdminAsync(client, "uc1");

        // Create carrier
        var createDto = new CreateCarrierDto
        {
            Name = "Update Test Carrier",
            Code = "UTC" + Guid.NewGuid().ToString("N")[..3].ToUpper(),
            IsActive = true
        };
        var createResp = await client.PostAsJsonAsync("/api/admin/shipments/carriers", createDto, TestHelper.JsonOptions);
        var content = await createResp.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        var carrierId = json.RootElement.GetProperty("carrierId").GetInt32();

        var updateDto = new UpdateCarrierDto
        {
            Name = "Updated Carrier Name",
            DefaultRate = 20.00m
        };

        var response = await client.PutAsJsonAsync($"/api/admin/shipments/carriers/{carrierId}", updateDto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updateContent = await response.Content.ReadAsStringAsync();
        updateContent.Should().Contain("Carrier updated successfully");
    }

    [Fact]
    public async Task DeleteCarrier_NotInUse_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsAdminAsync(client, "dc1");

        // Create carrier
        var createDto = new CreateCarrierDto
        {
            Name = "Delete Test Carrier",
            Code = "DTC" + Guid.NewGuid().ToString("N")[..3].ToUpper(),
            IsActive = true
        };
        var createResp = await client.PostAsJsonAsync("/api/admin/shipments/carriers", createDto, TestHelper.JsonOptions);
        var content = await createResp.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        var carrierId = json.RootElement.GetProperty("carrierId").GetInt32();

        var response = await client.DeleteAsync($"/api/admin/shipments/carriers/{carrierId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var deleteContent = await response.Content.ReadAsStringAsync();
        deleteContent.Should().Contain("Carrier deleted");
    }

    [Fact]
    public async Task ToggleCarrierActive_AsAdmin_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsAdminAsync(client, "tca1");

        var createDto = new CreateCarrierDto
        {
            Name = "Toggle Test Carrier",
            Code = "TCA" + Guid.NewGuid().ToString("N")[..3].ToUpper(),
            IsActive = true
        };
        var createResp = await client.PostAsJsonAsync("/api/admin/shipments/carriers", createDto, TestHelper.JsonOptions);
        var content = await createResp.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        var carrierId = json.RootElement.GetProperty("carrierId").GetInt32();

        // Toggle off
        var response1 = await client.PatchAsync($"/api/admin/shipments/carriers/{carrierId}/toggle-active", null);
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        var content1 = await response1.Content.ReadAsStringAsync();
        content1.Should().Contain("Carrier deactivated");

        // Toggle on
        var response2 = await client.PatchAsync($"/api/admin/shipments/carriers/{carrierId}/toggle-active", null);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
        var content2 = await response2.Content.ReadAsStringAsync();
        content2.Should().Contain("Carrier activated");
    }

    [Fact]
    public async Task CreateCarrier_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var dto = new CreateCarrierDto { Name = "Test" };

        var response = await client.PostAsJsonAsync("/api/admin/shipments/carriers", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
