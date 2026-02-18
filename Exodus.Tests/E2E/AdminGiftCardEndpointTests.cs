using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Exodus.Controllers.Admin;
using Exodus.Models.Entities;
using FluentAssertions;
using Xunit;

namespace Exodus.Tests.E2E;

public class AdminGiftCardEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AdminGiftCardEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ==========================================
    // AUTHORIZATION
    // ==========================================

    [Fact]
    public async Task GetGiftCards_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/admin/gift-cards");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetGiftCards_AsCustomer_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsCustomerAsync(client, "gccust");

        var response = await client.GetAsync("/api/admin/gift-cards");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetGiftCards_AsSeller_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsSellerAsync(client, "gcsell");

        var response = await client.GetAsync("/api/admin/gift-cards");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ==========================================
    // CREATE GIFT CARD
    // ==========================================

    [Fact]
    public async Task CreateGiftCard_AsAdmin_ReturnsCreated()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsAdminAsync(client, "gcc1");

        var dto = new CreateGiftCardDto
        {
            InitialBalance = 100.00m,
            Currency = "TRY",
            RecipientEmail = "recipient@example.com",
            RecipientName = "Test Recipient",
            PersonalMessage = "Happy Birthday!"
        };

        var response = await client.PostAsJsonAsync("/api/admin/gift-cards", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Gift card created");
        content.Should().Contain("code");
    }

    [Fact]
    public async Task CreateGiftCard_WithCustomCode_ReturnsCreated()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsAdminAsync(client, "gcc2");

        var customCode = "CUSTOM-TEST-" + Guid.NewGuid().ToString("N")[..6].ToUpper();
        var dto = new CreateGiftCardDto
        {
            Code = customCode,
            InitialBalance = 250.00m,
            Currency = "TRY"
        };

        var response = await client.PostAsJsonAsync("/api/admin/gift-cards", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain(customCode);
    }

    [Fact]
    public async Task CreateGiftCard_WithExpiration_ReturnsCreated()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsAdminAsync(client, "gcc3");

        var dto = new CreateGiftCardDto
        {
            InitialBalance = 50.00m,
            ExpiresAt = DateTime.UtcNow.AddMonths(6),
            AdminNotes = "Test gift card with expiration"
        };

        var response = await client.PostAsJsonAsync("/api/admin/gift-cards", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateGiftCard_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var dto = new CreateGiftCardDto { InitialBalance = 100.00m };

        var response = await client.PostAsJsonAsync("/api/admin/gift-cards", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ==========================================
    // CREATE BULK GIFT CARDS
    // ==========================================

    [Fact]
    public async Task CreateBulkGiftCards_AsAdmin_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsAdminAsync(client, "gcb1");

        var dto = new CreateBulkGiftCardsDto
        {
            Count = 3,
            InitialBalance = 50.00m,
            Currency = "TRY",
            AdminNotes = "Bulk test"
        };

        var response = await client.PostAsJsonAsync("/api/admin/gift-cards/bulk", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        json.RootElement.GetProperty("codes").GetArrayLength().Should().Be(3);
    }

    // ==========================================
    // GET GIFT CARDS
    // ==========================================

    [Fact]
    public async Task GetGiftCards_AsAdmin_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsAdminAsync(client, "gcg1");

        // Create a gift card first
        await client.PostAsJsonAsync("/api/admin/gift-cards",
            new CreateGiftCardDto { InitialBalance = 100.00m }, TestHelper.JsonOptions);

        var response = await client.GetAsync("/api/admin/gift-cards");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        json.RootElement.GetProperty("items").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetGiftCards_WithPagination_ReturnsPagedResults()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsAdminAsync(client, "gcg2");

        var response = await client.GetAsync("/api/admin/gift-cards?page=1&pageSize=5");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        json.RootElement.GetProperty("page").GetInt32().Should().Be(1);
        json.RootElement.GetProperty("pageSize").GetInt32().Should().Be(5);
    }

    // ==========================================
    // GET GIFT CARD BY ID
    // ==========================================

    [Fact]
    public async Task GetGiftCard_ExistingId_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsAdminAsync(client, "gcgi1");

        var createResp = await client.PostAsJsonAsync("/api/admin/gift-cards",
            new CreateGiftCardDto { InitialBalance = 75.00m }, TestHelper.JsonOptions);
        var content = await createResp.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        var id = json.RootElement.GetProperty("id").GetInt32();

        var response = await client.GetAsync($"/api/admin/gift-cards/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var detailContent = await response.Content.ReadAsStringAsync();
        detailContent.Should().Contain("initialBalance");
        detailContent.Should().Contain("currentBalance");
    }

    // ==========================================
    // UPDATE STATUS
    // ==========================================

    [Fact]
    public async Task UpdateStatus_AsAdmin_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsAdminAsync(client, "gcus1");

        var createResp = await client.PostAsJsonAsync("/api/admin/gift-cards",
            new CreateGiftCardDto { InitialBalance = 100.00m }, TestHelper.JsonOptions);
        var content = await createResp.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        var id = json.RootElement.GetProperty("id").GetInt32();

        var dto = new UpdateGiftCardStatusDto
        {
            Status = GiftCardStatus.Suspended,
            Notes = "Suspended for testing"
        };

        var response = await client.PatchAsJsonAsync($"/api/admin/gift-cards/{id}/status", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updateContent = await response.Content.ReadAsStringAsync();
        updateContent.Should().Contain("Gift card status updated");
    }

    // ==========================================
    // ADJUST BALANCE
    // ==========================================

    [Fact]
    public async Task AdjustBalance_AddBalance_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsAdminAsync(client, "gcab1");

        var createResp = await client.PostAsJsonAsync("/api/admin/gift-cards",
            new CreateGiftCardDto { InitialBalance = 100.00m }, TestHelper.JsonOptions);
        var content = await createResp.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        var id = json.RootElement.GetProperty("id").GetInt32();

        var dto = new AdjustGiftCardBalanceDto
        {
            Amount = 50.00m,
            Description = "Bonus credit"
        };

        var response = await client.PatchAsJsonAsync($"/api/admin/gift-cards/{id}/balance", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updateContent = await response.Content.ReadAsStringAsync();
        var updateJson = JsonDocument.Parse(updateContent);
        updateJson.RootElement.GetProperty("newBalance").GetDecimal().Should().Be(150.00m);
    }

    [Fact]
    public async Task AdjustBalance_ReduceBalance_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsAdminAsync(client, "gcab2");

        var createResp = await client.PostAsJsonAsync("/api/admin/gift-cards",
            new CreateGiftCardDto { InitialBalance = 100.00m }, TestHelper.JsonOptions);
        var content = await createResp.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        var id = json.RootElement.GetProperty("id").GetInt32();

        var dto = new AdjustGiftCardBalanceDto
        {
            Amount = -30.00m,
            Description = "Manual deduction"
        };

        var response = await client.PatchAsJsonAsync($"/api/admin/gift-cards/{id}/balance", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updateContent = await response.Content.ReadAsStringAsync();
        var updateJson = JsonDocument.Parse(updateContent);
        updateJson.RootElement.GetProperty("newBalance").GetDecimal().Should().Be(70.00m);
    }

    // ==========================================
    // EXTEND EXPIRATION
    // ==========================================

    [Fact]
    public async Task ExtendExpiration_AsAdmin_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsAdminAsync(client, "gcee1");

        var createResp = await client.PostAsJsonAsync("/api/admin/gift-cards",
            new CreateGiftCardDto
            {
                InitialBalance = 100.00m,
                ExpiresAt = DateTime.UtcNow.AddDays(30)
            }, TestHelper.JsonOptions);
        var content = await createResp.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        var id = json.RootElement.GetProperty("id").GetInt32();

        var newExpiry = DateTime.UtcNow.AddMonths(12);
        var dto = new ExtendGiftCardDto { NewExpiresAt = newExpiry };

        var response = await client.PatchAsJsonAsync($"/api/admin/gift-cards/{id}/extend", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updateContent = await response.Content.ReadAsStringAsync();
        updateContent.Should().Contain("Gift card expiration extended");
    }

    // ==========================================
    // STATISTICS
    // ==========================================

    [Fact]
    public async Task GetStatistics_AsAdmin_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsAdminAsync(client, "gcst1");

        // Create some gift cards
        await client.PostAsJsonAsync("/api/admin/gift-cards",
            new CreateGiftCardDto { InitialBalance = 100.00m }, TestHelper.JsonOptions);

        var response = await client.GetAsync("/api/admin/gift-cards/statistics");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("totalGiftCards");
        content.Should().Contain("totalIssuedValue");
    }

    [Fact]
    public async Task GetStatistics_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/admin/gift-cards/statistics");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ==========================================
    // GET BY CODE
    // ==========================================

    [Fact]
    public async Task GetGiftCardByCode_ExistingCode_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsAdminAsync(client, "gcbc1");

        var customCode = "TEST-" + Guid.NewGuid().ToString("N")[..8].ToUpper();
        var createResp = await client.PostAsJsonAsync("/api/admin/gift-cards",
            new CreateGiftCardDto { Code = customCode, InitialBalance = 100.00m }, TestHelper.JsonOptions);
        createResp.EnsureSuccessStatusCode();

        var response = await client.GetAsync($"/api/admin/gift-cards/code/{customCode}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
