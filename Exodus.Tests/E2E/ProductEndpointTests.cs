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

    // Products are a shared catalog: several sellers list the same product. Editing or deleting a
    // catalog entry is therefore an Admin action; sellers manage only their own listings.

    private async Task<int> CreateProductAsSellerAsync(string suffix)
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsSellerAsync(client, suffix);
        var dto = new AddProductDto
        {
            ProductName = "Catalog Product " + suffix,
            ProductDescription = "Shared catalog entry",
            Barcodes = new List<string> { Guid.NewGuid().ToString("N") }
        };
        var response = await client.PostAsJsonAsync("/api/product", dto, TestHelper.JsonOptions);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<ProductResponseDto>(TestHelper.JsonOptions);
        return created!.Id;
    }

    private static ProductUpdateDto RenameTo(string name) => new()
    {
        ProductName = name,
        ProductDescription = "Updated description",
        Barcodes = new List<string> { Guid.NewGuid().ToString("N") }
    };

    private async Task<string> GetProductNameAsync(int id)
    {
        var response = await _factory.CreateClient().GetAsync($"/api/product/{id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var product = await response.Content.ReadFromJsonAsync<ProductResponseDto>(TestHelper.JsonOptions);
        return product!.ProductName;
    }

    [Fact]
    public async Task UpdateProduct_AsAnotherSeller_ReturnsForbidden_AndProductIsUnchanged()
    {
        var id = await CreateProductAsSellerAsync("produpdowner");
        var rival = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsSellerAsync(rival, "produpdrival");

        var response = await rival.PutAsJsonAsync($"/api/product/{id}", RenameTo("Hijacked"), TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await GetProductNameAsync(id)).Should().Be("Catalog Product produpdowner");
    }

    [Fact]
    public async Task UpdateProduct_AsCreatingSeller_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsSellerAsync(client, "produpdself");
        var dto = new AddProductDto
        {
            ProductName = "Own Product",
            ProductDescription = "Created by this seller",
            Barcodes = new List<string> { Guid.NewGuid().ToString("N") }
        };
        var created = await (await client.PostAsJsonAsync("/api/product", dto, TestHelper.JsonOptions))
            .Content.ReadFromJsonAsync<ProductResponseDto>(TestHelper.JsonOptions);

        var response = await client.PutAsJsonAsync($"/api/product/{created!.Id}", RenameTo("Renamed"), TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateProduct_AsAdmin_ReturnsOk()
    {
        var id = await CreateProductAsSellerAsync("produpdadmin");
        var admin = await TestHelper.CreateAdminClientAsync(_factory);

        var response = await admin.PutAsJsonAsync($"/api/product/{id}", RenameTo("Updated Name"), TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await GetProductNameAsync(id)).Should().Be("Updated Name");
    }

    [Fact]
    public async Task DeleteProduct_AsAnotherSeller_ReturnsForbidden_AndProductStillExists()
    {
        var id = await CreateProductAsSellerAsync("proddelowner");
        var rival = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsSellerAsync(rival, "proddelrival");

        var response = await rival.DeleteAsync($"/api/product/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await GetProductNameAsync(id)).Should().Be("Catalog Product proddelowner");
    }

    [Fact]
    public async Task DeleteProduct_AsAdmin_ReturnsNoContent()
    {
        var id = await CreateProductAsSellerAsync("proddeladmin");
        var admin = await TestHelper.CreateAdminClientAsync(_factory);

        var response = await admin.DeleteAsync($"/api/product/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await _factory.CreateClient().GetAsync($"/api/product/{id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
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
