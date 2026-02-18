using System.Net;
using System.Net.Http.Json;
using Exodus.Services.SellerReviews;
using FluentAssertions;
using Xunit;

namespace Exodus.Tests.E2E;

public class SellerReviewEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public SellerReviewEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetSellerReviews_AllowAnonymous_ReturnsOk()
    {
        var client = _factory.CreateClient();

        // Create a seller so we have a valid sellerId
        var seller = await TestHelper.RegisterAndLoginAsSellerAsync(client, "srlistseller");
        client.DefaultRequestHeaders.Authorization = null;

        var response = await client.GetAsync($"/api/sellers/{seller.UserId}/reviews");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetSellerReviews_WithPagination_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var seller = await TestHelper.RegisterAndLoginAsSellerAsync(client, "srpageseller");
        client.DefaultRequestHeaders.Authorization = null;

        var response = await client.GetAsync($"/api/sellers/{seller.UserId}/reviews?page=1&pageSize=5");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetRatingSummary_AllowAnonymous_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var seller = await TestHelper.RegisterAndLoginAsSellerAsync(client, "srratingseller");
        client.DefaultRequestHeaders.Authorization = null;

        var response = await client.GetAsync($"/api/sellers/{seller.UserId}/rating-summary");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateReview_WithAuth_ReturnsCreated()
    {
        var client = _factory.CreateClient();
        var seller = await TestHelper.RegisterAndLoginAsSellerAsync(client, "srcreateseller");

        // Switch to customer to write a review
        await TestHelper.RegisterAndLoginAsCustomerAsync(client, "srcreate");

        var dto = new CreateSellerReviewDto
        {
            SellerId = seller.UserId,
            Rating = 5,
            Comment = "Excellent seller, fast shipping!",
            ShippingRating = 5,
            CommunicationRating = 4,
            PackagingRating = 5
        };

        var response = await client.PostAsJsonAsync($"/api/sellers/{seller.UserId}/reviews", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateReview_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var dto = new CreateSellerReviewDto
        {
            SellerId = 1,
            Rating = 5,
            Comment = "Should not work"
        };

        var response = await client.PostAsJsonAsync("/api/sellers/1/reviews", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ReportReview_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/api/sellers/1/reviews/1/report", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
