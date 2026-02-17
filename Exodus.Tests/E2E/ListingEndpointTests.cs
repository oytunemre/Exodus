using System.Net;
using System.Net.Http.Json;
using Exodus.Models.Dto.ListingDto;
using Exodus.Models.Dto.ProductDto;
using Exodus.Models.Enums;
using FluentAssertions;
using Xunit;

namespace Exodus.Tests.E2E;

public class ListingEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ListingEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<int> CreateProductAsSellerAsync(HttpClient client, int sellerId)
    {
        var dto = new AddProductDto
        {
            ProductName = "Listing Product " + Guid.NewGuid().ToString("N")[..8],
            ProductDescription = "Product for listing tests",
            Barcodes = new List<string> { Guid.NewGuid().ToString("N") }
        };

        var response = await client.PostAsJsonAsync("/api/product", dto, TestHelper.JsonOptions);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ProductResponseDto>(TestHelper.JsonOptions);
        return result!.Id;
    }

    [Fact]
    public async Task GetAllListings_ReturnsOk()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/listings");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateListing_AsSeller_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var seller = await TestHelper.RegisterAndLoginAsSellerAsync(client, "lst1");
        var productId = await CreateProductAsSellerAsync(client, seller.UserId);

        var dto = new AddListingDto
        {
            ProductId = productId,
            SellerId = seller.UserId,
            Price = 99.99m,
            Stock = 50,
            Condition = ListingCondition.New
        };

        var response = await client.PostAsJsonAsync("/api/listings", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ListingResponseDto>(TestHelper.JsonOptions);
        result.Should().NotBeNull();
        result!.ProductId.Should().Be(productId);
        result.Price.Should().Be(99.99m);
        result.Stock.Should().Be(50);
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CreateListing_AsCustomer_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsCustomerAsync(client, "lstcust");

        var dto = new AddListingDto
        {
            ProductId = 1,
            SellerId = 1,
            Price = 50.00m,
            Stock = 10
        };

        var response = await client.PostAsJsonAsync("/api/listings", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetListingById_WithValidId_ReturnsListing()
    {
        var client = _factory.CreateClient();
        var seller = await TestHelper.RegisterAndLoginAsSellerAsync(client, "lstget");
        var productId = await CreateProductAsSellerAsync(client, seller.UserId);

        var createDto = new AddListingDto
        {
            ProductId = productId,
            SellerId = seller.UserId,
            Price = 149.99m,
            Stock = 25,
            Condition = ListingCondition.New
        };

        var createResponse = await client.PostAsJsonAsync("/api/listings", createDto, TestHelper.JsonOptions);
        var created = await createResponse.Content.ReadFromJsonAsync<ListingResponseDto>(TestHelper.JsonOptions);

        // Public endpoint
        client.DefaultRequestHeaders.Authorization = null;
        var response = await client.GetAsync($"/api/listings/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ListingResponseDto>(TestHelper.JsonOptions);
        result!.Price.Should().Be(149.99m);
    }

    [Fact]
    public async Task UpdateListing_AsSeller_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var seller = await TestHelper.RegisterAndLoginAsSellerAsync(client, "lstupd");
        var productId = await CreateProductAsSellerAsync(client, seller.UserId);

        var createDto = new AddListingDto
        {
            ProductId = productId,
            SellerId = seller.UserId,
            Price = 100.00m,
            Stock = 30,
            Condition = ListingCondition.New
        };

        var createResponse = await client.PostAsJsonAsync("/api/listings", createDto, TestHelper.JsonOptions);
        var created = await createResponse.Content.ReadFromJsonAsync<ListingResponseDto>(TestHelper.JsonOptions);

        var updateDto = new UpdateListingDto
        {
            Price = 89.99m,
            Stock = 40,
            Condition = ListingCondition.LikeNew,
            IsActive = true
        };

        var response = await client.PutAsJsonAsync($"/api/listings/{created!.Id}", updateDto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ListingResponseDto>(TestHelper.JsonOptions);
        result!.Price.Should().Be(89.99m);
        result.Stock.Should().Be(40);
    }

    [Fact]
    public async Task DeleteListing_AsSeller_ReturnsNoContent()
    {
        var client = _factory.CreateClient();
        var seller = await TestHelper.RegisterAndLoginAsSellerAsync(client, "lstdel");
        var productId = await CreateProductAsSellerAsync(client, seller.UserId);

        var createDto = new AddListingDto
        {
            ProductId = productId,
            SellerId = seller.UserId,
            Price = 75.00m,
            Stock = 15,
            Condition = ListingCondition.Used
        };

        var createResponse = await client.PostAsJsonAsync("/api/listings", createDto, TestHelper.JsonOptions);
        var created = await createResponse.Content.ReadFromJsonAsync<ListingResponseDto>(TestHelper.JsonOptions);

        var response = await client.DeleteAsync($"/api/listings/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task CreateListing_WithDifferentConditions_Succeeds()
    {
        var client = _factory.CreateClient();
        var seller = await TestHelper.RegisterAndLoginAsSellerAsync(client, "lstcond");

        var conditions = new[] { ListingCondition.New, ListingCondition.LikeNew, ListingCondition.Used, ListingCondition.Refurbished };

        foreach (var condition in conditions)
        {
            var productId = await CreateProductAsSellerAsync(client, seller.UserId);

            var dto = new AddListingDto
            {
                ProductId = productId,
                SellerId = seller.UserId,
                Price = 50.00m,
                Stock = 10,
                Condition = condition
            };

            var response = await client.PostAsJsonAsync("/api/listings", dto, TestHelper.JsonOptions);

            response.StatusCode.Should().Be(HttpStatusCode.OK,
                $"Creating listing with condition {condition} should succeed");
            var result = await response.Content.ReadFromJsonAsync<ListingResponseDto>(TestHelper.JsonOptions);
            result!.Condition.Should().Be(condition);
        }
    }
}
