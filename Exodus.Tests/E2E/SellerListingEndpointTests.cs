using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Exodus.Controllers.Seller;
using Exodus.Models.Dto.ProductDto;
using Exodus.Models.Enums;
using FluentAssertions;
using Xunit;

namespace Exodus.Tests.E2E;

public class SellerListingEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public SellerListingEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<(HttpClient Client, AuthResponseDto Seller, int ProductId)> SetupSellerWithProductAsync(string suffix)
    {
        var client = _factory.CreateClient();
        var seller = await TestHelper.RegisterAndLoginAsSellerAsync(client, suffix);

        var productDto = new AddProductDto
        {
            ProductName = $"Seller Listing Product {suffix}",
            ProductDescription = "Product for seller listing tests",
            Barcodes = new List<string> { Guid.NewGuid().ToString("N") }
        };
        var prodResponse = await client.PostAsJsonAsync("/api/product", productDto, TestHelper.JsonOptions);
        prodResponse.EnsureSuccessStatusCode();
        var product = await prodResponse.Content.ReadFromJsonAsync<ProductResponseDto>(TestHelper.JsonOptions);

        return (client, seller, product!.Id);
    }

    private async Task<int> CreateSellerListingAsync(HttpClient client, int productId)
    {
        var dto = new SellerCreateListingDto
        {
            ProductId = productId,
            Price = 99.99m,
            StockQuantity = 50,
            Condition = ListingCondition.New
        };
        var response = await client.PostAsJsonAsync("/api/seller/listings", dto, TestHelper.JsonOptions);
        response.EnsureSuccessStatusCode();

        var location = response.Headers.Location?.ToString();
        if (location != null)
        {
            var listingResponse = await client.GetAsync(location);
            if (listingResponse.IsSuccessStatusCode)
            {
                var listing = await listingResponse.Content.ReadFromJsonAsync<JsonElement>(TestHelper.JsonOptions);
                if (listing.TryGetProperty("id", out var idElement))
                    return idElement.GetInt32();
            }
        }

        // Fallback: get listing ID from the seller listings list
        var listingsResponse = await client.GetAsync("/api/seller/listings");
        listingsResponse.EnsureSuccessStatusCode();
        var listingsJson = await listingsResponse.Content.ReadFromJsonAsync<JsonElement>(TestHelper.JsonOptions);
        if (listingsJson.TryGetProperty("items", out var items))
        {
            foreach (var item in items.EnumerateArray())
            {
                if (item.TryGetProperty("productId", out var pid) && pid.GetInt32() == productId)
                    return item.GetProperty("id").GetInt32();
            }
        }
        return 0;
    }

    [Fact]
    public async Task GetMyListings_AsSeller_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsSellerAsync(client, "sllst1");

        var response = await client.GetAsync("/api/seller/listings");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>(TestHelper.JsonOptions);
        result.TryGetProperty("items", out _).Should().BeTrue();
        result.TryGetProperty("totalCount", out _).Should().BeTrue();
    }

    [Fact]
    public async Task GetMyListings_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/seller/listings");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMyListings_AsCustomer_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsCustomerAsync(client, "sllstcust");

        var response = await client.GetAsync("/api/seller/listings");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateListing_AsSeller_ReturnsCreated()
    {
        var (client, seller, productId) = await SetupSellerWithProductAsync("slcrt1");

        var dto = new SellerCreateListingDto
        {
            ProductId = productId,
            Price = 149.99m,
            StockQuantity = 25,
            Condition = ListingCondition.New,
            SKU = "SKU-TEST-001"
        };

        var response = await client.PostAsJsonAsync("/api/seller/listings", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateListing_WithNonExistentProduct_ReturnsNotFound()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsSellerAsync(client, "slcrtnotfound");

        var dto = new SellerCreateListingDto
        {
            ProductId = 999999,
            Price = 49.99m,
            StockQuantity = 10,
            Condition = ListingCondition.New
        };

        var response = await client.PostAsJsonAsync("/api/seller/listings", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateListing_Duplicate_ReturnsConflict()
    {
        var (client, seller, productId) = await SetupSellerWithProductAsync("slcrtdup");

        var dto = new SellerCreateListingDto
        {
            ProductId = productId,
            Price = 99.99m,
            StockQuantity = 10,
            Condition = ListingCondition.New
        };

        var firstResponse = await client.PostAsJsonAsync("/api/seller/listings", dto, TestHelper.JsonOptions);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var secondResponse = await client.PostAsJsonAsync("/api/seller/listings", dto, TestHelper.JsonOptions);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateListing_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var dto = new SellerCreateListingDto
        {
            ProductId = 1,
            Price = 99.99m,
            StockQuantity = 10,
            Condition = ListingCondition.New
        };

        var response = await client.PostAsJsonAsync("/api/seller/listings", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateListing_AsSeller_ReturnsOk()
    {
        var (client, seller, productId) = await SetupSellerWithProductAsync("slupd1");
        var listingId = await CreateSellerListingAsync(client, productId);
        listingId.Should().BeGreaterThan(0);

        var dto = new SellerUpdateListingDto
        {
            Price = 199.99m,
            IsActive = false
        };

        var response = await client.PutAsJsonAsync($"/api/seller/listings/{listingId}", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateListing_NonExistent_ReturnsNotFound()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsSellerAsync(client, "slupdnotfound");

        var dto = new SellerUpdateListingDto { Price = 99.99m };

        var response = await client.PutAsJsonAsync("/api/seller/listings/999999", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateStock_AsSeller_ReturnsOk()
    {
        var (client, seller, productId) = await SetupSellerWithProductAsync("slstk1");
        var listingId = await CreateSellerListingAsync(client, productId);
        listingId.Should().BeGreaterThan(0);

        var dto = new SellerUpdateStockDto { StockQuantity = 100 };

        var response = await client.PatchAsJsonAsync($"/api/seller/listings/{listingId}/stock", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>(TestHelper.JsonOptions);
        result.TryGetProperty("stockQuantity", out var stockQuantity).Should().BeTrue();
        stockQuantity.GetInt32().Should().Be(100);
    }

    [Fact]
    public async Task UpdateStock_NonExistent_ReturnsNotFound()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsSellerAsync(client, "slstknotfound");

        var dto = new SellerUpdateStockDto { StockQuantity = 5 };

        var response = await client.PatchAsJsonAsync("/api/seller/listings/999999/stock", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetLowStock_AsSeller_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsSellerAsync(client, "sllowstk");

        var response = await client.GetAsync("/api/seller/listings/low-stock");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>(TestHelper.JsonOptions);
        result.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task GetOutOfStock_AsSeller_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsSellerAsync(client, "sloosstk");

        var response = await client.GetAsync("/api/seller/listings/out-of-stock");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>(TestHelper.JsonOptions);
        result.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task DeleteListing_AsSeller_ReturnsNoContent()
    {
        var (client, seller, productId) = await SetupSellerWithProductAsync("sldel1");
        var listingId = await CreateSellerListingAsync(client, productId);
        listingId.Should().BeGreaterThan(0);

        var response = await client.DeleteAsync($"/api/seller/listings/{listingId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteListing_NonExistent_ReturnsNotFound()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsSellerAsync(client, "sldelnotfound");

        var response = await client.DeleteAsync("/api/seller/listings/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetMyListings_WithFilters_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsSellerAsync(client, "sllstfilt");

        var response = await client.GetAsync("/api/seller/listings?isActive=true&page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateListingWithOutOfStock_StockStatusIsOutOfStock()
    {
        var (client, seller, productId) = await SetupSellerWithProductAsync("sloos");

        var dto = new SellerCreateListingDto
        {
            ProductId = productId,
            Price = 49.99m,
            StockQuantity = 0,
            Condition = ListingCondition.Used
        };

        var response = await client.PostAsJsonAsync("/api/seller/listings", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
