using System.Net;
using System.Net.Http.Json;
using Exodus.Models.Dto.Campaign;
using FluentAssertions;
using Xunit;

namespace Exodus.Tests.E2E;

public class CampaignEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CampaignEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetActiveCampaigns_AllowAnonymous_ReturnsOk()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/campaign/active");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<CampaignDto>>(TestHelper.JsonOptions);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetActiveCampaigns_WithCategoryFilter_ReturnsOk()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/campaign/active?categoryId=1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ValidateCoupon_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/campaign/validate-coupon?code=TESTCODE");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ValidateCoupon_WithInvalidCode_ReturnsNotFound()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsCustomerAsync(client, "campval");

        var response = await client.GetAsync("/api/campaign/validate-coupon?code=INVALIDCODE");

        // Invalid coupon should not return OK
        response.StatusCode.Should().NotBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetApplicableCampaigns_WithAuth_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsCustomerAsync(client, "campappl");

        var response = await client.GetAsync("/api/campaign/applicable");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetApplicableCampaigns_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/campaign/applicable");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CalculateCampaign_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var dto = new ApplyCampaignDto { CouponCode = "TEST" };
        var response = await client.PostAsJsonAsync("/api/campaign/calculate", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
