using Exodus.Data;
using Exodus.Models.Dto.ProductDto;
using Exodus.Models.Entities;
using Exodus.Services.Common;
using Exodus.Services.Products;
using Exodus.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Exodus.Tests.Services;

public class ProductServiceTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly ProductService _service;

    public ProductServiceTests()
    {
        _db = TestDbContextFactory.Create();
        _service = new ProductService(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    private async Task<Product> SeedProductAsync(string name = "Test Product", string desc = "Test description", params string[] barcodes)
    {
        var product = new Product
        {
            ProductName = name,
            ProductDescription = desc,
            Barcodes = (barcodes.Length > 0 ? barcodes : new[] { "BC-001" })
                .Select(b => new ProductBarcode { Barcode = b }).ToList()
        };
        _db.Products.Add(product);
        await _db.SaveChangesAsync();
        return product;
    }

    #region GetAllAsync

    [Fact]
    public async Task GetAllAsync_WhenNoProducts_ShouldReturnEmptyList()
    {
        var result = await _service.GetAllAsync();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllProducts()
    {
        await SeedProductAsync("Product 1", "Desc 1", "BC-001");
        await SeedProductAsync("Product 2", "Desc 2", "BC-002");

        var result = await _service.GetAllAsync();
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllAsync_ShouldIncludeBarcodes()
    {
        await SeedProductAsync("Product 1", "Desc 1", "BC-A", "BC-B");

        var result = await _service.GetAllAsync();
        result[0].Barcodes.Should().HaveCount(2);
        result[0].Barcodes.Should().Contain("BC-A");
        result[0].Barcodes.Should().Contain("BC-B");
    }

    #endregion

    #region GetByIdAsync

    [Fact]
    public async Task GetByIdAsync_WhenExists_ShouldReturnProduct()
    {
        var product = await SeedProductAsync();

        var result = await _service.GetByIdAsync(product.Id);
        result.ProductName.Should().Be("Test Product");
        result.ProductDescription.Should().Be("Test description");
        result.Barcodes.Should().Contain("BC-001");
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotExists_ShouldThrowNotFoundException()
    {
        var act = () => _service.GetByIdAsync(999);
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Product not found.");
    }

    #endregion

    #region CreateAsync

    [Fact]
    public async Task CreateAsync_WithValidData_ShouldCreateProduct()
    {
        var dto = new AddProductDto
        {
            ProductName = "New Product",
            ProductDescription = "New description",
            Barcodes = new List<string> { "NEW-BC-001" }
        };

        var result = await _service.CreateAsync(dto);

        result.ProductName.Should().Be("New Product");
        result.ProductDescription.Should().Be("New description");
        result.Barcodes.Should().Contain("NEW-BC-001");
        result.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateAsync_WithMultipleBarcodes_ShouldCreateAll()
    {
        var dto = new AddProductDto
        {
            ProductName = "Multi Barcode Product",
            ProductDescription = "Description",
            Barcodes = new List<string> { "BC-1", "BC-2", "BC-3" }
        };

        var result = await _service.CreateAsync(dto);
        result.Barcodes.Should().HaveCount(3);
    }

    [Fact]
    public async Task CreateAsync_WithEmptyBarcodes_ShouldThrowBadRequestException()
    {
        var dto = new AddProductDto
        {
            ProductName = "No Barcode Product",
            ProductDescription = "Description",
            Barcodes = new List<string>()
        };

        var act = () => _service.CreateAsync(dto);
        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("*barkod*");
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateBarcode_ShouldThrowConflictException()
    {
        await SeedProductAsync("Existing", "Desc", "EXISTING-BC");

        var dto = new AddProductDto
        {
            ProductName = "New Product",
            ProductDescription = "Description",
            Barcodes = new List<string> { "EXISTING-BC" }
        };

        var act = () => _service.CreateAsync(dto);
        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task CreateAsync_ShouldNormalizeBarcodes_TrimWhitespace()
    {
        var dto = new AddProductDto
        {
            ProductName = "Trimmed Product",
            ProductDescription = "Description",
            Barcodes = new List<string> { "  BC-TRIM  " }
        };

        var result = await _service.CreateAsync(dto);
        result.Barcodes.Should().Contain("BC-TRIM");
    }

    [Fact]
    public async Task CreateAsync_ShouldNormalizeBarcodes_RemoveEmpties()
    {
        var dto = new AddProductDto
        {
            ProductName = "Product",
            ProductDescription = "Description",
            Barcodes = new List<string> { "BC-1", "", "  ", "BC-2" }
        };

        var result = await _service.CreateAsync(dto);
        result.Barcodes.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateAsync_ShouldDeduplicateBarcodes()
    {
        var dto = new AddProductDto
        {
            ProductName = "Product",
            ProductDescription = "Description",
            Barcodes = new List<string> { "BC-1", "bc-1", "BC-1" }
        };

        var result = await _service.CreateAsync(dto);
        result.Barcodes.Should().HaveCount(1);
    }

    #endregion

    #region UpdateAsync

    [Fact]
    public async Task UpdateAsync_ShouldUpdateNameAndDescription()
    {
        var product = await SeedProductAsync("Old Name", "Old Desc", "BC-001");

        var dto = new ProductUpdateDto
        {
            ProductName = "New Name",
            ProductDescription = "New Description",
            Barcodes = new List<string> { "BC-001" }
        };

        var result = await _service.UpdateAsync(product.Id, dto);
        result.ProductName.Should().Be("New Name");
        result.ProductDescription.Should().Be("New Description");
    }

    [Fact]
    public async Task UpdateAsync_WhenNotExists_ShouldThrowNotFoundException()
    {
        var dto = new ProductUpdateDto
        {
            ProductName = "Test",
            ProductDescription = "Test",
            Barcodes = new List<string> { "BC-001" }
        };

        var act = () => _service.UpdateAsync(999, dto);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_WithNewBarcode_ShouldAddBarcode()
    {
        var product = await SeedProductAsync("Product", "Desc", "BC-001");

        var dto = new ProductUpdateDto
        {
            ProductName = "Product",
            ProductDescription = "Desc",
            Barcodes = new List<string> { "BC-001", "BC-NEW" }
        };

        var result = await _service.UpdateAsync(product.Id, dto);
        result.Barcodes.Should().HaveCount(2);
        result.Barcodes.Should().Contain("BC-NEW");
    }

    [Fact]
    public async Task UpdateAsync_RemovingBarcode_ShouldSoftDeleteBarcode()
    {
        var product = await SeedProductAsync("Product", "Desc", "BC-001", "BC-002");

        var dto = new ProductUpdateDto
        {
            ProductName = "Product",
            ProductDescription = "Desc",
            Barcodes = new List<string> { "BC-001" }
        };

        var result = await _service.UpdateAsync(product.Id, dto);
        result.Barcodes.Should().HaveCount(1);
        result.Barcodes.Should().NotContain("BC-002");

        // Verify soft delete
        var deletedBarcode = await _db.ProductBarcodes
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(b => b.Barcode == "BC-002");
        deletedBarcode.Should().NotBeNull();
        deletedBarcode!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_WithBarcodeUsedByOtherProduct_ShouldThrowConflictException()
    {
        var product1 = await SeedProductAsync("Product 1", "Desc", "BC-001");
        var product2 = await SeedProductAsync("Product 2", "Desc", "BC-002");

        var dto = new ProductUpdateDto
        {
            ProductName = "Product 2",
            ProductDescription = "Desc",
            Barcodes = new List<string> { "BC-001" }
        };

        var act = () => _service.UpdateAsync(product2.Id, dto);
        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task UpdateAsync_WithEmptyBarcodes_ShouldThrowBadRequestException()
    {
        var product = await SeedProductAsync();

        var dto = new ProductUpdateDto
        {
            ProductName = "Product",
            ProductDescription = "Desc",
            Barcodes = new List<string>()
        };

        var act = () => _service.UpdateAsync(product.Id, dto);
        await act.Should().ThrowAsync<BadRequestException>();
    }

    #endregion

    #region SoftDeleteAsync

    [Fact]
    public async Task SoftDeleteAsync_WhenExists_ShouldMarkAsDeleted()
    {
        var product = await SeedProductAsync();

        await _service.SoftDeleteAsync(product.Id);

        var deleted = await _db.Products.IgnoreQueryFilters()
            .FirstAsync(p => p.Id == product.Id);
        deleted.IsDeleted.Should().BeTrue();
        deleted.DeletedDate.Should().NotBeNull();
    }

    [Fact]
    public async Task SoftDeleteAsync_WhenNotExists_ShouldThrowNotFoundException()
    {
        var act = () => _service.SoftDeleteAsync(999);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    #endregion
}
