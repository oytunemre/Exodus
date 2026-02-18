using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Exodus.Models.Dto;
using Exodus.Models.Dto.CartDto;
using Exodus.Models.Dto.ListingDto;
using Exodus.Models.Dto.Payment;
using Exodus.Models.Dto.ProductDto;
using Exodus.Models.Enums;
using FluentAssertions;
using Xunit;

namespace Exodus.Tests.E2E;

public class AdminPaymentEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AdminPaymentEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<(HttpClient Client, int PaymentIntentId, int OrderId)> CreatePaymentAsync(string suffix)
    {
        var client = _factory.CreateClient();

        // Seller creates product + listing
        var seller = await TestHelper.RegisterAndLoginAsSellerAsync(client, $"admpay{suffix}");
        var prodDto = new AddProductDto
        {
            ProductName = $"Admin Pay Test {suffix}",
            ProductDescription = "For admin payment tests",
            Barcodes = new List<string> { Guid.NewGuid().ToString("N") }
        };
        var prodResp = await client.PostAsJsonAsync("/api/product", prodDto, TestHelper.JsonOptions);
        var prod = await prodResp.Content.ReadFromJsonAsync<ProductResponseDto>(TestHelper.JsonOptions);

        var listingDto = new AddListingDto
        {
            ProductId = prod!.Id,
            SellerId = seller.UserId,
            Price = 200.00m,
            Stock = 50,
            Condition = ListingCondition.New
        };
        var lstResp = await client.PostAsJsonAsync("/api/listings", listingDto, TestHelper.JsonOptions);
        var listing = await lstResp.Content.ReadFromJsonAsync<ListingResponseDto>(TestHelper.JsonOptions);

        // Customer creates order
        var customer = await TestHelper.RegisterAndLoginAsCustomerAsync(client, $"admpay{suffix}");
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

        // Create payment intent
        var payDto = new CreatePaymentIntentDto
        {
            OrderId = order!.Id,
            Method = PaymentMethod.CreditCard,
            Currency = "TRY"
        };
        var payResp = await client.PostAsJsonAsync("/api/payment/intents", payDto, TestHelper.JsonOptions);
        var payment = await payResp.Content.ReadFromJsonAsync<PaymentIntentResponseDto>(TestHelper.JsonOptions);

        return (client, payment!.Id, order.Id);
    }

    // ==========================================
    // AUTHORIZATION
    // ==========================================

    [Fact]
    public async Task GetPayments_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/admin/payments");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPayments_AsCustomer_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsCustomerAsync(client, "apcust");

        var response = await client.GetAsync("/api/admin/payments");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetPayments_AsSeller_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsSellerAsync(client, "apsell");

        var response = await client.GetAsync("/api/admin/payments");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ==========================================
    // GET PAYMENTS
    // ==========================================

    [Fact]
    public async Task GetPayments_AsAdmin_ReturnsOk()
    {
        var (client, _, _) = await CreatePaymentAsync("gp1");

        // Switch to admin
        await TestHelper.RegisterAndLoginAsAdminAsync(client, "gp1");

        var response = await client.GetAsync("/api/admin/payments");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Items");
        content.Should().Contain("TotalCount");
    }

    [Fact]
    public async Task GetPayments_WithPagination_ReturnsPagedResults()
    {
        var (client, _, _) = await CreatePaymentAsync("gp2");
        await TestHelper.RegisterAndLoginAsAdminAsync(client, "gp2");

        var response = await client.GetAsync("/api/admin/payments?page=1&pageSize=5");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        json.RootElement.GetProperty("page").GetInt32().Should().Be(1);
        json.RootElement.GetProperty("pageSize").GetInt32().Should().Be(5);
    }

    [Fact]
    public async Task GetPayments_WithStatusFilter_ReturnsFiltered()
    {
        var (client, _, _) = await CreatePaymentAsync("gp3");
        await TestHelper.RegisterAndLoginAsAdminAsync(client, "gp3");

        var response = await client.GetAsync("/api/admin/payments?status=Created");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ==========================================
    // GET SINGLE PAYMENT
    // ==========================================

    [Fact]
    public async Task GetPayment_ExistingId_ReturnsOk()
    {
        var (client, paymentId, _) = await CreatePaymentAsync("gs1");
        await TestHelper.RegisterAndLoginAsAdminAsync(client, "gs1");

        var response = await client.GetAsync($"/api/admin/payments/{paymentId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Payment");
        content.Should().Contain("Events");
    }

    // ==========================================
    // GET PAYMENT EVENTS
    // ==========================================

    [Fact]
    public async Task GetPaymentEvents_AsAdmin_ReturnsOk()
    {
        var (client, paymentId, _) = await CreatePaymentAsync("pe1");
        await TestHelper.RegisterAndLoginAsAdminAsync(client, "pe1");

        var response = await client.GetAsync($"/api/admin/payments/events?paymentId={paymentId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetPaymentEvents_WithEventTypeFilter_ReturnsOk()
    {
        var (client, _, _) = await CreatePaymentAsync("pe2");
        await TestHelper.RegisterAndLoginAsAdminAsync(client, "pe2");

        var response = await client.GetAsync("/api/admin/payments/events?eventType=payment.created");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ==========================================
    // FAILED PAYMENTS
    // ==========================================

    [Fact]
    public async Task GetFailedPayments_AsAdmin_ReturnsOk()
    {
        var (client, paymentId, _) = await CreatePaymentAsync("fp1");

        // Fail the payment
        await client.PostAsJsonAsync($"/api/payment/intents/{paymentId}/simulate-fail",
            new SimulatePaymentDto { Reason = "Test failure" }, TestHelper.JsonOptions);

        await TestHelper.RegisterAndLoginAsAdminAsync(client, "fp1");

        var response = await client.GetAsync("/api/admin/payments/failed");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Items");
    }

    // ==========================================
    // STATISTICS
    // ==========================================

    [Fact]
    public async Task GetStatistics_AsAdmin_ReturnsOk()
    {
        var (client, _, _) = await CreatePaymentAsync("stat1");
        await TestHelper.RegisterAndLoginAsAdminAsync(client, "stat1");

        var response = await client.GetAsync("/api/admin/payments/statistics");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Total");
        content.Should().Contain("Successful");
        content.Should().Contain("Failed");
    }

    [Fact]
    public async Task GetStatistics_WithDateRange_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsAdminAsync(client, "stat2");

        var from = DateTime.UtcNow.AddDays(-7).ToString("yyyy-MM-dd");
        var to = DateTime.UtcNow.ToString("yyyy-MM-dd");

        var response = await client.GetAsync($"/api/admin/payments/statistics?fromDate={from}&toDate={to}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetStatistics_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/admin/payments/statistics");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
