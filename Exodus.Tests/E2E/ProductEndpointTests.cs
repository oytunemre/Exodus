using System.Net;
using System.Net.Http.Json;
using Exodus.Models.Dto.ProductDto;
using Exodus.Models.Enums;
using FluentAssertions;
using Xunit;

namespace Exodus.Tests.E2E;

public class ProductEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ProductEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAllProducts_ReturnsOk()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/product");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateProduct_AsSeller_ReturnsCreated()
    {
        var client = _factory.CreateClient();
        var seller = await TestHelper.RegisterAndLoginAsSellerAsync(client, "prod1");

        var dto = new AddProductDto
        {
            ProductName = "Test Product",
            ProductDescription = "A test product description",
            Barcodes = new List<string> { "1234567890123" }
        };

        var response = await client.PostAsJsonAsync("/api/product", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ProductResponseDto>(TestHelper.JsonOptions);
        result.Should().NotBeNull();
        result!.ProductName.Should().Be("Test Product");
        result.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateProduct_AsAdmin_ReturnsCreated()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsAdminAsync(client);

        var dto = new AddProductDto
        {
            ProductName = "Admin Product",
            ProductDescription = "Product created by admin",
            Barcodes = new List<string> { "9876543210123" }
        };

        var response = await client.PostAsJsonAsync("/api/product", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateProduct_AsCustomer_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsCustomerAsync(client, "prodcust");

        var dto = new AddProductDto
        {
            ProductName = "Customer Product",
            ProductDescription = "Should not be created",
            Barcodes = new List<string>()
        };

        var response = await client.PostAsJsonAsync("/api/product", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateProduct_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var dto = new AddProductDto
        {
            ProductName = "Anon Product",
            ProductDescription = "Should fail",
            Barcodes = new List<string>()
        };

        var response = await client.PostAsJsonAsync("/api/product", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProductById_WithValidId_ReturnsProduct()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsSellerAsync(client, "prodget");

        var createDto = new AddProductDto
        {
            ProductName = "Get By Id Product",
            ProductDescription = "Test description",
            Barcodes = new List<string> { "5555555555555" }
        };

        var createResponse = await client.PostAsJsonAsync("/api/product", createDto, TestHelper.JsonOptions);
        var created = await createResponse.Content.ReadFromJsonAsync<ProductResponseDto>(TestHelper.JsonOptions);

        // Clear auth - product read is anonymous
        client.DefaultRequestHeaders.Authorization = null;

        var response = await client.GetAsync($"/api/product/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ProductResponseDto>(TestHelper.JsonOptions);
        result!.ProductName.Should().Be("Get By Id Product");
    }

    [Fact]
    public async Task UpdateProduct_AsSeller_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsSellerAsync(client, "produpd");

        var createDto = new AddProductDto
        {
            ProductName = "Original Name",
            ProductDescription = "Original description",
            Barcodes = new List<string> { Guid.NewGuid().ToString("N") }
        };

        var createResponse = await client.PostAsJsonAsync("/api/product", createDto, TestHelper.JsonOptions);
        var created = await createResponse.Content.ReadFromJsonAsync<ProductResponseDto>(TestHelper.JsonOptions);

        var updateDto = new ProductUpdateDto
        {
            ProductName = "Updated Name",
            ProductDescription = "Updated description",
            Barcodes = new List<string> { "1111111111111" }
        };

        var response = await client.PutAsJsonAsync($"/api/product/{created!.Id}", updateDto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ProductResponseDto>(TestHelper.JsonOptions);
        result!.ProductName.Should().Be("Updated Name");
    }

    [Fact]
    public async Task DeleteProduct_AsSeller_ReturnsNoContent()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsSellerAsync(client, "proddel");

        var createDto = new AddProductDto
        {
            ProductName = "To Be Deleted",
            ProductDescription = "Will be deleted",
            Barcodes = new List<string> { Guid.NewGuid().ToString("N") }
        };

        var createResponse = await client.PostAsJsonAsync("/api/product", createDto, TestHelper.JsonOptions);
        var created = await createResponse.Content.ReadFromJsonAsync<ProductResponseDto>(TestHelper.JsonOptions);

        var response = await client.DeleteAsync($"/api/product/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteProduct_AsCustomer_ReturnsForbidden()
    {
        var client = _factory.CreateClient();

        // Create product as seller
        await TestHelper.RegisterAndLoginAsSellerAsync(client, "proddelcust");
        var createDto = new AddProductDto
        {
            ProductName = "Cannot Delete",
            ProductDescription = "Customer cannot delete",
            Barcodes = new List<string> { Guid.NewGuid().ToString("N") }
        };
        var createResponse = await client.PostAsJsonAsync("/api/product", createDto, TestHelper.JsonOptions);
        var created = await createResponse.Content.ReadFromJsonAsync<ProductResponseDto>(TestHelper.JsonOptions);

        // Switch to customer
        await TestHelper.RegisterAndLoginAsCustomerAsync(client, "proddelcust");

        var response = await client.DeleteAsync($"/api/product/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
