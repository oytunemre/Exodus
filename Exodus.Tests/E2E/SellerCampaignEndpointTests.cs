using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Exodus.Models.Dto.Campaign;
using Exodus.Models.Entities;
using FluentAssertions;
using Xunit;

namespace Exodus.Tests.E2E;

public class SellerCampaignEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public SellerCampaignEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static CreateCampaignDto BuildCampaignDto(string suffix) => new()
    {
        Name = $"Test Campaign {suffix}",
        Description = "A test campaign",
        Type = CampaignType.PercentageDiscount,
        StartDate = DateTime.UtcNow.AddDays(-1),
        EndDate = DateTime.UtcNow.AddDays(30),
        IsActive = true,
        DiscountPercentage = 10,
        Scope = CampaignScope.AllProducts
    };

    private async Task<(HttpClient Client, int CampaignId)> CreateSellerCampaignAsync(string suffix)
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsSellerAsync(client, suffix);

        var dto = BuildCampaignDto(suffix);
        var response = await client.PostAsJsonAsync("/api/seller/campaigns", dto, TestHelper.JsonOptions);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var campaign = await response.Content.ReadFromJsonAsync<CampaignDto>(TestHelper.JsonOptions);
        return (client, campaign!.Id);
    }

    [Fact]
    public async Task CreateCampaign_AsSeller_ReturnsCreated()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsSellerAsync(client, "slcmp1");

        var dto = BuildCampaignDto("slcmp1");
        var response = await client.PostAsJsonAsync("/api/seller/campaigns", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var campaign = await response.Content.ReadFromJsonAsync<CampaignDto>(TestHelper.JsonOptions);
        campaign.Should().NotBeNull();
        campaign!.Name.Should().Contain("slcmp1");
        campaign.Type.Should().Be(CampaignType.PercentageDiscount);
    }

    [Fact]
    public async Task CreateCampaign_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var dto = BuildCampaignDto("noauth");

        var response = await client.PostAsJsonAsync("/api/seller/campaigns", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateCampaign_AsCustomer_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsCustomerAsync(client, "slcmpcust");

        var dto = BuildCampaignDto("slcmpcust");
        var response = await client.PostAsJsonAsync("/api/seller/campaigns", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAll_AsSeller_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsSellerAsync(client, "slcmpall");

        var response = await client.GetAsync("/api/seller/campaigns");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var campaigns = await response.Content.ReadFromJsonAsync<List<CampaignDto>>(TestHelper.JsonOptions);
        campaigns.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAll_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/seller/campaigns");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_IncludeInactive_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsSellerAsync(client, "slcmpallinact");

        var response = await client.GetAsync("/api/seller/campaigns?includeInactive=true");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetById_OwnCampaign_ReturnsOk()
    {
        var (client, campaignId) = await CreateSellerCampaignAsync("slcmpget");

        var response = await client.GetAsync($"/api/seller/campaigns/{campaignId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var campaign = await response.Content.ReadFromJsonAsync<CampaignDto>(TestHelper.JsonOptions);
        campaign.Should().NotBeNull();
        campaign!.Id.Should().Be(campaignId);
    }

    [Fact]
    public async Task GetById_OtherSellerCampaign_ReturnsForbidden()
    {
        var (client1, campaignId) = await CreateSellerCampaignAsync("slcmpown1");

        // Register a different seller and try to access the first seller's campaign
        var client2 = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsSellerAsync(client2, "slcmpown2");

        var response = await client2.GetAsync($"/api/seller/campaigns/{campaignId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Update_OwnCampaign_ReturnsOk()
    {
        var (client, campaignId) = await CreateSellerCampaignAsync("slcmpupd");

        var dto = new UpdateCampaignDto
        {
            Name = "Updated Campaign Name",
            DiscountPercentage = 15
        };

        var response = await client.PutAsJsonAsync($"/api/seller/campaigns/{campaignId}", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var campaign = await response.Content.ReadFromJsonAsync<CampaignDto>(TestHelper.JsonOptions);
        campaign.Should().NotBeNull();
        campaign!.Name.Should().Be("Updated Campaign Name");
        campaign.DiscountPercentage.Should().Be(15);
    }

    [Fact]
    public async Task Update_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var dto = new UpdateCampaignDto { Name = "Updated" };
        var response = await client.PutAsJsonAsync("/api/seller/campaigns/1", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Delete_OwnCampaign_ReturnsNoContent()
    {
        var (client, campaignId) = await CreateSellerCampaignAsync("slcmpdel");

        var response = await client.DeleteAsync($"/api/seller/campaigns/{campaignId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.DeleteAsync("/api/seller/campaigns/1");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ToggleActive_OwnCampaign_ReturnsOk()
    {
        var (client, campaignId) = await CreateSellerCampaignAsync("slcmptoggle");

        var response = await client.PatchAsync($"/api/seller/campaigns/{campaignId}/toggle-active", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var campaign = await response.Content.ReadFromJsonAsync<CampaignDto>(TestHelper.JsonOptions);
        campaign.Should().NotBeNull();
        // After toggle, IsActive should change (was true, now false)
        campaign!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task ToggleActive_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PatchAsync("/api/seller/campaigns/1/toggle-active", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetStatistics_OwnCampaign_ReturnsOk()
    {
        var (client, campaignId) = await CreateSellerCampaignAsync("slcmpstat");

        var response = await client.GetAsync($"/api/seller/campaigns/{campaignId}/statistics");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stats = await response.Content.ReadFromJsonAsync<CampaignStatisticsDto>(TestHelper.JsonOptions);
        stats.Should().NotBeNull();
        stats!.CampaignId.Should().Be(campaignId);
    }

    [Fact]
    public async Task GetUsageHistory_OwnCampaign_ReturnsOk()
    {
        var (client, campaignId) = await CreateSellerCampaignAsync("slcmpusage");

        var response = await client.GetAsync($"/api/seller/campaigns/{campaignId}/usage");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var usage = await response.Content.ReadFromJsonAsync<List<CampaignUsageDto>>(TestHelper.JsonOptions);
        usage.Should().NotBeNull();
        usage.Should().BeEmpty(); // no orders have used this campaign yet
    }
}
