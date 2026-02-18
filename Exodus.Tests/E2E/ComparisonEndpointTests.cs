using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Exodus.Models.Dto.ProductDto;
using FluentAssertions;
using Xunit;

namespace Exodus.Tests.E2E;

public class ComparisonEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ComparisonEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<int> CreateTestProductAsync(HttpClient client, string suffix)
    {
        await TestHelper.RegisterAndLoginAsSellerAsync(client, suffix);
        var dto = new AddProductDto
        {
            ProductName = "Compare Product " + suffix,
            ProductDescription = "Product for comparison tests",
            Barcodes = new List<string> { Guid.NewGuid().ToString("N") }
        };
        var resp = await client.PostAsJsonAsync("/api/product", dto, TestHelper.JsonOptions);
        var product = await resp.Content.ReadFromJsonAsync<ProductResponseDto>(TestHelper.JsonOptions);
        return product!.Id;
    }

    [Fact]
    public async Task GetComparisons_WithAuth_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsCustomerAsync(client, "cmplist");

        var response = await client.GetAsync("/api/comparison");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetComparisons_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/comparison");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateComparison_ReturnsCreated()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsCustomerAsync(client, "cmpcreate");

        var dto = new { Name = "Phone Comparison" };
        var response = await client.PostAsJsonAsync("/api/comparison", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task AddProductToComparison_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var productId = await CreateTestProductAsync(client, "cmpadd");

        await TestHelper.RegisterAndLoginAsCustomerAsync(client, "cmpadd");

        // Create comparison
        var createDto = new { Name = "Test Comparison" };
        var createResp = await client.PostAsJsonAsync("/api/comparison", createDto, TestHelper.JsonOptions);
        var content = await createResp.Content.ReadAsStringAsync();
        var created = JsonDocument.Parse(content).RootElement;
        var comparisonId = created.GetProperty("id").GetInt32();

        // Add product
        var response = await client.PostAsync($"/api/comparison/{comparisonId}/products/{productId}", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RemoveProductFromComparison_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var productId = await CreateTestProductAsync(client, "cmpremove");

        await TestHelper.RegisterAndLoginAsCustomerAsync(client, "cmpremove");

        // Create comparison and add product
        var createResp = await client.PostAsJsonAsync("/api/comparison", new { Name = "Remove Test" }, TestHelper.JsonOptions);
        var content = await createResp.Content.ReadAsStringAsync();
        var created = JsonDocument.Parse(content).RootElement;
        var comparisonId = created.GetProperty("id").GetInt32();

        await client.PostAsync($"/api/comparison/{comparisonId}/products/{productId}", null);

        // Remove product
        var response = await client.DeleteAsync($"/api/comparison/{comparisonId}/products/{productId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetComparisonById_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsCustomerAsync(client, "cmpget");

        var createResp = await client.PostAsJsonAsync("/api/comparison", new { Name = "Get Test" }, TestHelper.JsonOptions);
        var content = await createResp.Content.ReadAsStringAsync();
        var created = JsonDocument.Parse(content).RootElement;
        var comparisonId = created.GetProperty("id").GetInt32();

        var response = await client.GetAsync($"/api/comparison/{comparisonId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteComparison_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsCustomerAsync(client, "cmpdel");

        var createResp = await client.PostAsJsonAsync("/api/comparison", new { Name = "Delete Test" }, TestHelper.JsonOptions);
        var content = await createResp.Content.ReadAsStringAsync();
        var created = JsonDocument.Parse(content).RootElement;
        var comparisonId = created.GetProperty("id").GetInt32();

        var response = await client.DeleteAsync($"/api/comparison/{comparisonId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetDetailedComparison_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsCustomerAsync(client, "cmpdetail");

        var createResp = await client.PostAsJsonAsync("/api/comparison", new { Name = "Detail Test" }, TestHelper.JsonOptions);
        var content = await createResp.Content.ReadAsStringAsync();
        var created = JsonDocument.Parse(content).RootElement;
        var comparisonId = created.GetProperty("id").GetInt32();

        var response = await client.GetAsync($"/api/comparison/{comparisonId}/detailed");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
