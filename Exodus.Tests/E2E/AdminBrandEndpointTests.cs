using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Exodus.Controllers.Admin;
using FluentAssertions;
using Xunit;

namespace Exodus.Tests.E2E;

public class AdminBrandEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AdminBrandEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<(HttpClient Client, int BrandId, string BrandSlug)> CreateBrandAsync(string suffix)
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsAdminAsync(client, $"brand{suffix}");

        var dto = new CreateBrandDto
        {
            Name = $"Test Brand {suffix}",
            Description = "A test brand",
            Website = "https://example.com",
            IsActive = true,
            IsFeatured = false
        };

        var response = await client.PostAsJsonAsync("/api/admin/brands", dto, TestHelper.JsonOptions);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        var id = json.RootElement.GetProperty("id").GetInt32();
        var slug = json.RootElement.GetProperty("slug").GetString()!;

        return (client, id, slug);
    }

    // ==========================================
    // AUTHORIZATION
    // ==========================================

    [Fact]
    public async Task GetBrands_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/admin/brands");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetBrands_AsCustomer_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsCustomerAsync(client, "brcust");

        var response = await client.GetAsync("/api/admin/brands");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetBrands_AsSeller_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsSellerAsync(client, "brsell");

        var response = await client.GetAsync("/api/admin/brands");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ==========================================
    // CREATE BRAND
    // ==========================================

    [Fact]
    public async Task CreateBrand_AsAdmin_ReturnsCreated()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsAdminAsync(client, "brc1");

        var dto = new CreateBrandDto
        {
            Name = "Apple",
            Description = "Consumer electronics",
            Website = "https://apple.com",
            IsActive = true,
            IsFeatured = true
        };

        var response = await client.PostAsJsonAsync("/api/admin/brands", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        json.RootElement.GetProperty("name").GetString().Should().Be("Apple");
        json.RootElement.GetProperty("slug").GetString().Should().Be("apple");
    }

    [Fact]
    public async Task CreateBrand_WithCustomSlug_ReturnsCreated()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsAdminAsync(client, "brc2");

        var dto = new CreateBrandDto
        {
            Name = "Samsung Electronics",
            Slug = "samsung",
            Description = "Korean electronics",
            IsActive = true
        };

        var response = await client.PostAsJsonAsync("/api/admin/brands", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        json.RootElement.GetProperty("slug").GetString().Should().Be("samsung");
    }

    [Fact]
    public async Task CreateBrand_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var dto = new CreateBrandDto { Name = "Test" };

        var response = await client.PostAsJsonAsync("/api/admin/brands", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ==========================================
    // GET BRANDS
    // ==========================================

    [Fact]
    public async Task GetBrands_AsAdmin_ReturnsOk()
    {
        var (client, _, _) = await CreateBrandAsync("gb1");

        var response = await client.GetAsync("/api/admin/brands");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        json.RootElement.GetProperty("items").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetBrands_WithSearch_ReturnsFiltered()
    {
        var (client, _, _) = await CreateBrandAsync("gb2");

        var response = await client.GetAsync("/api/admin/brands?search=Test+Brand+gb2");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetBrands_WithPagination_ReturnsPagedResults()
    {
        var (client, _, _) = await CreateBrandAsync("gb3");

        var response = await client.GetAsync("/api/admin/brands?page=1&pageSize=5");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        json.RootElement.GetProperty("page").GetInt32().Should().Be(1);
        json.RootElement.GetProperty("pageSize").GetInt32().Should().Be(5);
    }

    // ==========================================
    // GET BRAND BY ID
    // ==========================================

    [Fact]
    public async Task GetBrand_ExistingId_ReturnsOk()
    {
        var (client, brandId, _) = await CreateBrandAsync("gbi1");

        var response = await client.GetAsync($"/api/admin/brands/{brandId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Test Brand gbi1");
    }

    // ==========================================
    // GET BRAND BY SLUG
    // ==========================================

    [Fact]
    public async Task GetBrandBySlug_ExistingSlug_ReturnsOk()
    {
        var (client, _, slug) = await CreateBrandAsync("gbs1");

        var response = await client.GetAsync($"/api/admin/brands/slug/{slug}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ==========================================
    // UPDATE BRAND
    // ==========================================

    [Fact]
    public async Task UpdateBrand_AsAdmin_ReturnsOk()
    {
        var (client, brandId, _) = await CreateBrandAsync("ub1");

        var dto = new UpdateBrandDto
        {
            Name = "Updated Brand Name",
            Description = "Updated description",
            IsFeatured = true
        };

        var response = await client.PutAsJsonAsync($"/api/admin/brands/{brandId}", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Updated Brand Name");
    }

    // ==========================================
    // DELETE BRAND
    // ==========================================

    [Fact]
    public async Task DeleteBrand_AsAdmin_ReturnsOk()
    {
        var (client, brandId, _) = await CreateBrandAsync("db1");

        var response = await client.DeleteAsync($"/api/admin/brands/{brandId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Brand deleted");

        // Verify deleted
        var getResp = await client.GetAsync($"/api/admin/brands/{brandId}");
        getResp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteBrand_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.DeleteAsync("/api/admin/brands/1");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ==========================================
    // TOGGLE ACTIVE
    // ==========================================

    [Fact]
    public async Task ToggleActive_AsAdmin_ReturnsOk()
    {
        var (client, brandId, _) = await CreateBrandAsync("ta1");

        // Toggle off
        var response1 = await client.PatchAsync($"/api/admin/brands/{brandId}/toggle-active", null);
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        var content1 = await response1.Content.ReadAsStringAsync();
        content1.Should().Contain("Brand deactivated");

        // Toggle on
        var response2 = await client.PatchAsync($"/api/admin/brands/{brandId}/toggle-active", null);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
        var content2 = await response2.Content.ReadAsStringAsync();
        content2.Should().Contain("Brand activated");
    }

    // ==========================================
    // TOGGLE FEATURED
    // ==========================================

    [Fact]
    public async Task ToggleFeatured_AsAdmin_ReturnsOk()
    {
        var (client, brandId, _) = await CreateBrandAsync("tf1");

        // Toggle on
        var response1 = await client.PatchAsync($"/api/admin/brands/{brandId}/toggle-featured", null);
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        var content1 = await response1.Content.ReadAsStringAsync();
        content1.Should().Contain("Brand featured");

        // Toggle off
        var response2 = await client.PatchAsync($"/api/admin/brands/{brandId}/toggle-featured", null);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
        var content2 = await response2.Content.ReadAsStringAsync();
        content2.Should().Contain("Brand unfeatured");
    }

    // ==========================================
    // REORDER
    // ==========================================

    [Fact]
    public async Task ReorderBrands_AsAdmin_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsAdminAsync(client, "reorder");

        // Create two brands
        var resp1 = await client.PostAsJsonAsync("/api/admin/brands",
            new CreateBrandDto { Name = "Brand A Reorder" }, TestHelper.JsonOptions);
        var json1 = JsonDocument.Parse(await resp1.Content.ReadAsStringAsync());
        var id1 = json1.RootElement.GetProperty("id").GetInt32();

        var resp2 = await client.PostAsJsonAsync("/api/admin/brands",
            new CreateBrandDto { Name = "Brand B Reorder" }, TestHelper.JsonOptions);
        var json2 = JsonDocument.Parse(await resp2.Content.ReadAsStringAsync());
        var id2 = json2.RootElement.GetProperty("id").GetInt32();

        var orders = new List<BrandOrderDto>
        {
            new() { Id = id1, DisplayOrder = 2 },
            new() { Id = id2, DisplayOrder = 1 }
        };

        var response = await client.PostAsJsonAsync("/api/admin/brands/reorder", orders, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Brands reordered");
    }
}
