using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Exodus.Models.Dto;
using Exodus.Models.Dto.ProductDto;
using FluentAssertions;
using Xunit;

namespace Exodus.Tests.E2E;

public class SellerListingWriteTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public SellerListingWriteTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<(HttpClient Client, int ProductId)> SetupAsync()
    {
        var s = Guid.NewGuid().ToString("N")[..8];
        var client = _factory.CreateClient();
        var seller = await TestHelper.RegisterAndLoginAsSellerAsync(client, $"slw{s}");
        TestHelper.SetAuthToken(client, seller.Token!);
        var prodResp = await client.PostAsJsonAsync("/api/product", new AddProductDto
        {
            ProductName = $"SLW {s}",
            ProductDescription = "Seller listing write tests",
            Barcodes = new List<string> { Guid.NewGuid().ToString("N") }
        }, TestHelper.JsonOptions);
        var prod = await prodResp.Content.ReadFromJsonAsync<ProductResponseDto>(TestHelper.JsonOptions);
        return (client, prod!.Id);
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage resp)
        => JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement.Clone();

    private static async Task<JsonElement> FindInListAsync(HttpClient client, int id)
    {
        var resp = await client.GetAsync("/api/seller/listings");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var root = await ReadJsonAsync(resp);
        return root.GetProperty("items").EnumerateArray().Single(i => i.GetProperty("id").GetInt32() == id);
    }

    private static async Task<int> CreateAsync(HttpClient client, int productId, decimal price = 100m, int stock = 10)
    {
        var resp = await client.PostAsJsonAsync("/api/seller/listings",
            new { productId, price, stockQuantity = stock }, TestHelper.JsonOptions);
        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await ReadJsonAsync(resp)).GetProperty("id").GetInt32();
    }

    [Fact]
    public async Task CreateListing_ReturnsCreatedListingBody()
    {
        var (client, productId) = await SetupAsync();

        var resp = await client.PostAsJsonAsync("/api/seller/listings",
            new { productId, price = 199.90m, stockQuantity = 5 }, TestHelper.JsonOptions);

        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await ReadJsonAsync(resp);
        var id = body.GetProperty("id").GetInt32();
        id.Should().BeGreaterThan(0);
        body.GetProperty("price").GetDecimal().Should().Be(199.90m);
        body.GetProperty("stockQuantity").GetInt32().Should().Be(5);
        body.GetProperty("stockStatus").GetString().Should().Be("InStock");
        body.GetProperty("condition").GetString().Should().Be("New");
        body.GetProperty("isActive").GetBoolean().Should().BeTrue();

        var listed = await FindInListAsync(client, id);
        listed.GetProperty("id").GetInt32().Should().Be(id);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task CreateListing_NonPositivePrice_Returns400(int price)
    {
        var (client, productId) = await SetupAsync();

        var resp = await client.PostAsJsonAsync("/api/seller/listings",
            new { productId, price, stockQuantity = 5 }, TestHelper.JsonOptions);

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ReadJsonAsync(resp)).GetProperty("detail").GetString().Should().Be("Price must be greater than 0.");
    }

    [Fact]
    public async Task CreateListing_NegativeStock_Returns400()
    {
        var (client, productId) = await SetupAsync();

        var resp = await client.PostAsJsonAsync("/api/seller/listings",
            new { productId, price = 10m, stockQuantity = -1 }, TestHelper.JsonOptions);

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ReadJsonAsync(resp)).GetProperty("detail").GetString().Should().Be("Stock quantity cannot be negative.");
    }

    [Fact]
    public async Task UpdateListing_NegativePrice_Returns400_AndPriceUnchanged()
    {
        var (client, productId) = await SetupAsync();
        var id = await CreateAsync(client, productId, price: 100m);

        var resp = await client.PutAsJsonAsync($"/api/seller/listings/{id}",
            new { price = -1m }, TestHelper.JsonOptions);

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await FindInListAsync(client, id)).GetProperty("price").GetDecimal().Should().Be(100m);
    }

    [Fact]
    public async Task UpdateListing_NegativeLowStockThreshold_Returns400()
    {
        var (client, productId) = await SetupAsync();
        var id = await CreateAsync(client, productId);

        var resp = await client.PutAsJsonAsync($"/api/seller/listings/{id}",
            new { lowStockThreshold = -1 }, TestHelper.JsonOptions);

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ReadJsonAsync(resp)).GetProperty("detail").GetString().Should().Be("Low stock threshold cannot be negative.");
    }

    [Fact]
    public async Task UpdateListing_NegativeStock_Returns400()
    {
        var (client, productId) = await SetupAsync();
        var id = await CreateAsync(client, productId, stock: 10);

        var resp = await client.PutAsJsonAsync($"/api/seller/listings/{id}",
            new { stockQuantity = -3 }, TestHelper.JsonOptions);

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await FindInListAsync(client, id)).GetProperty("stockQuantity").GetInt32().Should().Be(10);
    }

    [Fact]
    public async Task UpdateStock_Negative_Returns400_AndStockUnchanged()
    {
        var (client, productId) = await SetupAsync();
        var id = await CreateAsync(client, productId, stock: 10);

        var resp = await client.PatchAsJsonAsync($"/api/seller/listings/{id}/stock",
            new { stockQuantity = -1 }, TestHelper.JsonOptions);

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await FindInListAsync(client, id)).GetProperty("stockQuantity").GetInt32().Should().Be(10);
    }
}
