using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Exodus.Tests.E2E;

public class AdminDashboardEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AdminDashboardEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ==========================================
    // AUTHORIZATION TESTS
    // ==========================================

    [Fact]
    public async Task GetDashboard_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/admin/dashboard");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetDashboard_AsCustomer_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsCustomerAsync(client, "addbcust");

        var response = await client.GetAsync("/api/admin/dashboard");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetDashboard_AsSeller_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsSellerAsync(client, "addbsell");

        var response = await client.GetAsync("/api/admin/dashboard");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ==========================================
    // ADMIN DASHBOARD ENDPOINTS
    // ==========================================

    [Fact]
    public async Task GetDashboard_AsAdmin_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsAdminAsync(client, "addb1");

        var response = await client.GetAsync("/api/admin/dashboard");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetDashboard_AsAdmin_WithDateRange_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsAdminAsync(client, "addbdate");

        var from = DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-dd");
        var to = DateTime.UtcNow.ToString("yyyy-MM-dd");

        var response = await client.GetAsync($"/api/admin/dashboard?fromDate={from}&toDate={to}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetOverview_AsAdmin_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsAdminAsync(client, "addbov");

        var response = await client.GetAsync("/api/admin/dashboard/overview");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetOverview_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/admin/dashboard/overview");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetSalesStatistics_AsAdmin_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsAdminAsync(client, "addbsale");

        var response = await client.GetAsync("/api/admin/dashboard/sales");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetSalesStatistics_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/admin/dashboard/sales");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetOrderStatistics_AsAdmin_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsAdminAsync(client, "addbordstat");

        var response = await client.GetAsync("/api/admin/dashboard/orders");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetUserStatistics_AsAdmin_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsAdminAsync(client, "addbusrstat");

        var response = await client.GetAsync("/api/admin/dashboard/users");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetProductStatistics_AsAdmin_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsAdminAsync(client, "addbprodstat");

        var response = await client.GetAsync("/api/admin/dashboard/products");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetActivities_AsAdmin_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsAdminAsync(client, "addbact");

        var response = await client.GetAsync("/api/admin/dashboard/activities");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetReportsSales_AsAdmin_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsAdminAsync(client, "addbrptsale");

        var response = await client.GetAsync("/api/admin/dashboard/reports/sales");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetReportsSales_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/admin/dashboard/reports/sales");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetReportsInventory_AsAdmin_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsAdminAsync(client, "addbrptinv");

        var response = await client.GetAsync("/api/admin/dashboard/reports/inventory");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetReportsCustomers_AsAdmin_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsAdminAsync(client, "addbrptcust");

        var response = await client.GetAsync("/api/admin/dashboard/reports/customers");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetDashboard_AsAdmin_ReturnsValidJsonStructure()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsAdminAsync(client, "addbstruct");

        var response = await client.GetAsync("/api/admin/dashboard");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrWhiteSpace();
        var json = JsonDocument.Parse(content);
        json.RootElement.ValueKind.Should().Be(JsonValueKind.Object);
    }
}
