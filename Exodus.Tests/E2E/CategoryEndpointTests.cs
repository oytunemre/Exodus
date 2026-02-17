using System.Net;
using System.Net.Http.Json;
using Exodus.Models.Dto;
using FluentAssertions;
using Xunit;

namespace Exodus.Tests.E2E;

public class CategoryEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CategoryEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAllCategories_ReturnsOk()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/category");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetCategoryTree_ReturnsOk()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/category/tree");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AdminCreateCategory_WithValidData_ReturnsCreated()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsAdminAsync(client);

        var dto = new CreateCategoryDto
        {
            Name = "Electronics",
            Description = "Electronic devices and gadgets"
        };

        var response = await client.PostAsJsonAsync("/api/admin/categories", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<CategoryResponseDto>(TestHelper.JsonOptions);
        result.Should().NotBeNull();
        result!.Name.Should().Be("Electronics");
        result.Slug.Should().NotBeNullOrEmpty();
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task AdminCreateCategory_AsCustomer_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsCustomerAsync(client, "catcust");

        var dto = new CreateCategoryDto
        {
            Name = "Unauthorized Category"
        };

        var response = await client.PostAsJsonAsync("/api/admin/categories", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AdminCreateCategory_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var dto = new CreateCategoryDto
        {
            Name = "No Auth Category"
        };

        var response = await client.PostAsJsonAsync("/api/admin/categories", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AdminCreateSubCategory_WithParent_ReturnsCreatedWithParent()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsAdminAsync(client);

        // Create parent category
        var parentDto = new CreateCategoryDto
        {
            Name = "Parent Category",
            Description = "A parent category"
        };
        var parentResponse = await client.PostAsJsonAsync("/api/admin/categories", parentDto, TestHelper.JsonOptions);
        var parent = await parentResponse.Content.ReadFromJsonAsync<CategoryResponseDto>(TestHelper.JsonOptions);

        // Create child category
        var childDto = new CreateCategoryDto
        {
            Name = "Child Category",
            Description = "A child category",
            ParentCategoryId = parent!.Id
        };
        var childResponse = await client.PostAsJsonAsync("/api/admin/categories", childDto, TestHelper.JsonOptions);

        childResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var child = await childResponse.Content.ReadFromJsonAsync<CategoryResponseDto>(TestHelper.JsonOptions);
        child!.ParentCategoryId.Should().Be(parent.Id);
    }

    [Fact]
    public async Task GetCategoryById_WithValidId_ReturnsCategory()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsAdminAsync(client);

        var dto = new CreateCategoryDto
        {
            Name = "Fetched Category",
            Description = "To be fetched"
        };
        var createResponse = await client.PostAsJsonAsync("/api/admin/categories", dto, TestHelper.JsonOptions);
        var created = await createResponse.Content.ReadFromJsonAsync<CategoryResponseDto>(TestHelper.JsonOptions);

        // Public endpoint - no auth needed
        client.DefaultRequestHeaders.Authorization = null;
        var response = await client.GetAsync($"/api/category/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CategoryResponseDto>(TestHelper.JsonOptions);
        result!.Name.Should().Be("Fetched Category");
    }

    [Fact]
    public async Task AdminUpdateCategory_WithValidData_ReturnsUpdated()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsAdminAsync(client);

        var createDto = new CreateCategoryDto
        {
            Name = "Original Category",
            Description = "Original description"
        };
        var createResponse = await client.PostAsJsonAsync("/api/admin/categories", createDto, TestHelper.JsonOptions);
        var created = await createResponse.Content.ReadFromJsonAsync<CategoryResponseDto>(TestHelper.JsonOptions);

        var updateDto = new UpdateCategoryDto
        {
            Name = "Updated Category",
            Description = "Updated description"
        };

        var response = await client.PutAsJsonAsync($"/api/admin/categories/{created!.Id}", updateDto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CategoryResponseDto>(TestHelper.JsonOptions);
        result!.Name.Should().Be("Updated Category");
    }

    [Fact]
    public async Task AdminDeleteCategory_ReturnsNoContent()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsAdminAsync(client);

        var dto = new CreateCategoryDto
        {
            Name = "To Delete Category"
        };
        var createResponse = await client.PostAsJsonAsync("/api/admin/categories", dto, TestHelper.JsonOptions);
        var created = await createResponse.Content.ReadFromJsonAsync<CategoryResponseDto>(TestHelper.JsonOptions);

        var response = await client.DeleteAsync($"/api/admin/categories/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task AdminToggleCategoryActive_TogglesStatus()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsAdminAsync(client);

        var dto = new CreateCategoryDto
        {
            Name = "Toggle Category"
        };
        var createResponse = await client.PostAsJsonAsync("/api/admin/categories", dto, TestHelper.JsonOptions);
        var created = await createResponse.Content.ReadFromJsonAsync<CategoryResponseDto>(TestHelper.JsonOptions);

        // Category is active by default, toggling should deactivate it
        var response = await client.PatchAsync($"/api/admin/categories/{created!.Id}/toggle-active", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CategoryResponseDto>(TestHelper.JsonOptions);
        result!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task GetSubCategories_ReturnsChildCategories()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsAdminAsync(client);

        // Create parent
        var parentDto = new CreateCategoryDto { Name = "SubCat Parent" };
        var parentResp = await client.PostAsJsonAsync("/api/admin/categories", parentDto, TestHelper.JsonOptions);
        var parent = await parentResp.Content.ReadFromJsonAsync<CategoryResponseDto>(TestHelper.JsonOptions);

        // Create children
        for (int i = 1; i <= 3; i++)
        {
            var childDto = new CreateCategoryDto
            {
                Name = $"SubCat Child {i}",
                ParentCategoryId = parent!.Id
            };
            await client.PostAsJsonAsync("/api/admin/categories", childDto, TestHelper.JsonOptions);
        }

        // Fetch subcategories via public endpoint
        client.DefaultRequestHeaders.Authorization = null;
        var response = await client.GetAsync($"/api/category/{parent!.Id}/subcategories");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var results = await response.Content.ReadFromJsonAsync<List<CategoryResponseDto>>(TestHelper.JsonOptions);
        results.Should().NotBeNull();
        results!.Count.Should().Be(3);
    }
}
