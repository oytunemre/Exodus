using Exodus.Data;
using Exodus.Models.Dto;
using Exodus.Models.Entities;
using Exodus.Services.Categories;
using Exodus.Services.Common;
using Exodus.Tests.Helpers;
using FluentAssertions;
using Xunit;

namespace Exodus.Tests.Services;

public class CategoryServiceTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly CategoryService _service;

    public CategoryServiceTests()
    {
        _db = TestDbContextFactory.Create();
        _service = new CategoryService(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    private async Task<Category> SeedCategoryAsync(string name = "Electronics", string slug = "electronics", int? parentId = null, bool isActive = true)
    {
        var cat = new Category
        {
            Name = name,
            Slug = slug,
            IsActive = isActive,
            ParentCategoryId = parentId,
            Description = $"{name} category"
        };
        _db.Categories.Add(cat);
        await _db.SaveChangesAsync();
        return cat;
    }

    #region GetAllAsync

    [Fact]
    public async Task GetAllAsync_WhenNoCategories_ShouldReturnEmpty()
    {
        var result = await _service.GetAllAsync();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnActiveCategories()
    {
        await SeedCategoryAsync("Active 1", "active-1", isActive: true);
        await SeedCategoryAsync("Active 2", "active-2", isActive: true);
        await SeedCategoryAsync("Inactive", "inactive", isActive: false);

        var result = await _service.GetAllAsync();
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllAsync_WithIncludeInactive_ShouldReturnAll()
    {
        await SeedCategoryAsync("Active", "active", isActive: true);
        await SeedCategoryAsync("Inactive", "inactive", isActive: false);

        var result = await _service.GetAllAsync(includeInactive: true);
        result.Should().HaveCount(2);
    }

    #endregion

    #region GetByIdAsync

    [Fact]
    public async Task GetByIdAsync_WhenExists_ShouldReturnCategory()
    {
        var cat = await SeedCategoryAsync("Electronics", "electronics");

        var result = await _service.GetByIdAsync(cat.Id);
        result.Should().NotBeNull();
        result!.Name.Should().Be("Electronics");
        result.Slug.Should().Be("electronics");
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotExists_ShouldReturnNull()
    {
        var result = await _service.GetByIdAsync(999);
        result.Should().BeNull();
    }

    #endregion

    #region GetBySlugAsync

    [Fact]
    public async Task GetBySlugAsync_WhenExists_ShouldReturnCategory()
    {
        await SeedCategoryAsync("Electronics", "electronics");

        var result = await _service.GetBySlugAsync("electronics");
        result.Should().NotBeNull();
        result!.Name.Should().Be("Electronics");
    }

    [Fact]
    public async Task GetBySlugAsync_WhenNotExists_ShouldReturnNull()
    {
        var result = await _service.GetBySlugAsync("nonexistent");
        result.Should().BeNull();
    }

    #endregion

    #region CreateAsync

    [Fact]
    public async Task CreateAsync_WithValidData_ShouldCreateCategory()
    {
        var dto = new CreateCategoryDto
        {
            Name = "Clothing",
            Description = "Clothing category"
        };

        var result = await _service.CreateAsync(dto);

        result.Name.Should().Be("Clothing");
        result.Slug.Should().Be("clothing");
        result.IsActive.Should().BeTrue();
        result.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateAsync_ShouldGenerateSlugFromName()
    {
        var dto = new CreateCategoryDto
        {
            Name = "Home & Garden"
        };

        var result = await _service.CreateAsync(dto);
        result.Slug.Should().Be("home--garden");
    }

    [Fact]
    public async Task CreateAsync_WithParentCategory_ShouldSetParentId()
    {
        var parent = await SeedCategoryAsync("Parent", "parent");

        var dto = new CreateCategoryDto
        {
            Name = "Child",
            ParentCategoryId = parent.Id
        };

        var result = await _service.CreateAsync(dto);
        result.ParentCategoryId.Should().Be(parent.Id);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidParentId_ShouldThrowNotFoundException()
    {
        var dto = new CreateCategoryDto
        {
            Name = "Orphan",
            ParentCategoryId = 999
        };

        var act = () => _service.CreateAsync(dto);
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Parent category not found");
    }

    [Fact]
    public async Task CreateAsync_ShouldSetDisplayOrder()
    {
        var dto = new CreateCategoryDto
        {
            Name = "Ordered",
            DisplayOrder = 5
        };

        var result = await _service.CreateAsync(dto);
        result.DisplayOrder.Should().Be(5);
    }

    #endregion

    #region UpdateAsync

    [Fact]
    public async Task UpdateAsync_ShouldUpdateName()
    {
        var cat = await SeedCategoryAsync("Old Name", "old-name");

        var dto = new UpdateCategoryDto { Name = "New Name" };

        var result = await _service.UpdateAsync(cat.Id, dto);
        result.Name.Should().Be("New Name");
        result.Slug.Should().Be("new-name");
    }

    [Fact]
    public async Task UpdateAsync_WhenNotExists_ShouldThrowNotFoundException()
    {
        var dto = new UpdateCategoryDto { Name = "Test" };

        var act = () => _service.UpdateAsync(999, dto);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_SettingSelfAsParent_ShouldThrowBadRequestException()
    {
        var cat = await SeedCategoryAsync("Self", "self");

        var dto = new UpdateCategoryDto { ParentCategoryId = cat.Id };

        var act = () => _service.UpdateAsync(cat.Id, dto);
        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Category cannot be its own parent");
    }

    [Fact]
    public async Task UpdateAsync_CanDeactivateCategory()
    {
        var cat = await SeedCategoryAsync("Active", "active");

        var dto = new UpdateCategoryDto { IsActive = false };

        var result = await _service.UpdateAsync(cat.Id, dto);
        result.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_CanUpdateDisplayOrder()
    {
        var cat = await SeedCategoryAsync();

        var dto = new UpdateCategoryDto { DisplayOrder = 10 };

        var result = await _service.UpdateAsync(cat.Id, dto);
        result.DisplayOrder.Should().Be(10);
    }

    [Fact]
    public async Task UpdateAsync_CanUpdateDescription()
    {
        var cat = await SeedCategoryAsync();

        var dto = new UpdateCategoryDto { Description = "New description" };

        var result = await _service.UpdateAsync(cat.Id, dto);
        result.Description.Should().Be("New description");
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidParentId_ShouldThrowNotFoundException()
    {
        var cat = await SeedCategoryAsync();

        var dto = new UpdateCategoryDto { ParentCategoryId = 999 };

        var act = () => _service.UpdateAsync(cat.Id, dto);
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Parent category not found");
    }

    #endregion

    #region DeleteAsync

    [Fact]
    public async Task DeleteAsync_WhenExists_ShouldDeleteCategory()
    {
        var cat = await SeedCategoryAsync();

        await _service.DeleteAsync(cat.Id);

        var result = await _service.GetByIdAsync(cat.Id);
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WhenNotExists_ShouldThrowNotFoundException()
    {
        var act = () => _service.DeleteAsync(999);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteAsync_WithSubCategories_ShouldThrowBadRequestException()
    {
        var parent = await SeedCategoryAsync("Parent", "parent");
        await SeedCategoryAsync("Child", "child", parent.Id);

        var act = () => _service.DeleteAsync(parent.Id);
        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("*subcategories*");
    }

    [Fact]
    public async Task DeleteAsync_WithProducts_ShouldThrowBadRequestException()
    {
        var cat = await SeedCategoryAsync();

        _db.Products.Add(new Product
        {
            ProductName = "Test",
            ProductDescription = "Desc",
            CategoryId = cat.Id
        });
        await _db.SaveChangesAsync();

        var act = () => _service.DeleteAsync(cat.Id);
        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("*products*");
    }

    #endregion

    #region GetSubCategoriesAsync

    [Fact]
    public async Task GetSubCategoriesAsync_ShouldReturnOnlyChildren()
    {
        var parent = await SeedCategoryAsync("Parent", "parent");
        await SeedCategoryAsync("Child 1", "child-1", parent.Id);
        await SeedCategoryAsync("Child 2", "child-2", parent.Id);
        await SeedCategoryAsync("Other", "other");

        var result = await _service.GetSubCategoriesAsync(parent.Id);
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetSubCategoriesAsync_ShouldReturnOnlyActive()
    {
        var parent = await SeedCategoryAsync("Parent", "parent");
        await SeedCategoryAsync("Active Child", "active-child", parent.Id, isActive: true);
        await SeedCategoryAsync("Inactive Child", "inactive-child", parent.Id, isActive: false);

        var result = await _service.GetSubCategoriesAsync(parent.Id);
        result.Should().HaveCount(1);
    }

    #endregion

    #region GetTreeAsync

    [Fact]
    public async Task GetTreeAsync_ShouldBuildHierarchy()
    {
        var root = await SeedCategoryAsync("Root", "root");
        var child1 = await SeedCategoryAsync("Child 1", "child-1", root.Id);
        await SeedCategoryAsync("Grandchild", "grandchild", child1.Id);

        var result = (await _service.GetTreeAsync()).ToList();

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Root");
        result[0].Children.Should().HaveCount(1);
        result[0].Children[0].Name.Should().Be("Child 1");
        result[0].Children[0].Children.Should().HaveCount(1);
        result[0].Children[0].Children[0].Name.Should().Be("Grandchild");
    }

    [Fact]
    public async Task GetTreeAsync_ShouldOnlyIncludeActiveCategories()
    {
        var root = await SeedCategoryAsync("Root", "root");
        await SeedCategoryAsync("Active", "active", root.Id, true);
        await SeedCategoryAsync("Inactive", "inactive", root.Id, false);

        var result = (await _service.GetTreeAsync()).ToList();
        result[0].Children.Should().HaveCount(1);
    }

    #endregion
}
