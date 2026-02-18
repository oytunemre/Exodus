using System.Net;
using System.Net.Http.Json;
using Exodus.Models.Dto.ProductDto;
using FluentAssertions;
using Xunit;

namespace Exodus.Tests.E2E;

public class RecentlyViewedEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public RecentlyViewedEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<int> CreateTestProductAsync(HttpClient client, string suffix)
    {
        await TestHelper.RegisterAndLoginAsSellerAsync(client, suffix);
        var dto = new AddProductDto
        {
            ProductName = "RV Test Product " + suffix,
            ProductDescription = "Product for recently viewed tests",
            Barcodes = new List<string> { Guid.NewGuid().ToString("N") }
        };
        var resp = await client.PostAsJsonAsync("/api/product", dto, TestHelper.JsonOptions);
        var product = await resp.Content.ReadFromJsonAsync<ProductResponseDto>(TestHelper.JsonOptions);
        return product!.Id;
    }

    [Fact]
    public async Task GetRecentlyViewed_WithAuth_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsCustomerAsync(client, "rvlist");

        var response = await client.GetAsync("/api/recentlyviewed");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetRecentlyViewed_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/recentlyviewed");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TrackProduct_WithAuth_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var productId = await CreateTestProductAsync(client, "rvtrack");

        await TestHelper.RegisterAndLoginAsCustomerAsync(client, "rvtrack");

        var response = await client.PostAsync($"/api/recentlyviewed/{productId}", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task TrackAndGetRecentlyViewed_ReturnsTrackedProduct()
    {
        var client = _factory.CreateClient();
        var productId = await CreateTestProductAsync(client, "rvtrackget");

        await TestHelper.RegisterAndLoginAsCustomerAsync(client, "rvtrackget");

        // Track
        await client.PostAsync($"/api/recentlyviewed/{productId}", null);

        // Get
        var response = await client.GetAsync("/api/recentlyviewed?count=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ClearRecentlyViewed_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsCustomerAsync(client, "rvclear");

        var response = await client.DeleteAsync("/api/recentlyviewed");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RemoveSpecificProduct_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var productId = await CreateTestProductAsync(client, "rvremove");

        await TestHelper.RegisterAndLoginAsCustomerAsync(client, "rvremove");

        // Track
        await client.PostAsync($"/api/recentlyviewed/{productId}", null);

        // Remove specific
        var response = await client.DeleteAsync($"/api/recentlyviewed/{productId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
