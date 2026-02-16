using Exodus.Data;
using Exodus.Models.Dto.ListingDto;
using Exodus.Models.Entities;
using Exodus.Models.Enums;
using Exodus.Services.Common;
using Exodus.Services.Listings;
using Exodus.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Exodus.Tests.Services;

public class ListingServiceTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly ListingService _service;

    public ListingServiceTests()
    {
        _db = TestDbContextFactory.Create();
        _service = new ListingService(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    private async Task<(Product product, Users seller)> SeedDependenciesAsync()
    {
        var seller = new Users
        {
            Name = "Seller",
            Email = "seller@test.com",
            Password = "pass",
            Username = "seller",
            Role = UserRole.Seller
        };
        _db.Users.Add(seller);

        var product = new Product
        {
            ProductName = "Test Product",
            ProductDescription = "Test description",
            Barcodes = new List<ProductBarcode> { new() { Barcode = "BC-001" } }
        };
        _db.Products.Add(product);

        await _db.SaveChangesAsync();
        return (product, seller);
    }

    private async Task<Listing> SeedListingAsync(int productId, int sellerId, decimal price = 100m, int stock = 10)
    {
        var listing = new Listing
        {
            ProductId = productId,
            SellerId = sellerId,
            Price = price,
            StockQuantity = stock,
            IsActive = true,
            Condition = ListingCondition.New
        };
        _db.Listings.Add(listing);
        await _db.SaveChangesAsync();
        return listing;
    }

    #region GetAllAsync

    [Fact]
    public async Task GetAllAsync_WhenNoListings_ShouldReturnEmptyList()
    {
        var result = await _service.GetAllAsync();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllListings()
    {
        var (product, seller) = await SeedDependenciesAsync();
        await SeedListingAsync(product.Id, seller.Id, 100);
        await SeedListingAsync(product.Id, seller.Id, 200);

        var result = await _service.GetAllAsync();
        result.Should().HaveCount(2);
    }

    #endregion

    #region GetByIdAsync

    [Fact]
    public async Task GetByIdAsync_WhenExists_ShouldReturnListing()
    {
        var (product, seller) = await SeedDependenciesAsync();
        var listing = await SeedListingAsync(product.Id, seller.Id, 150m, 20);

        var result = await _service.GetByIdAsync(listing.Id);
        result.Price.Should().Be(150m);
        result.Stock.Should().Be(20);
        result.ProductId.Should().Be(product.Id);
        result.SellerId.Should().Be(seller.Id);
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotExists_ShouldThrowNotFoundException()
    {
        var act = () => _service.GetByIdAsync(999);
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Listing not found.");
    }

    #endregion

    #region CreateAsync

    [Fact]
    public async Task CreateAsync_WithValidData_ShouldCreateListing()
    {
        var (product, seller) = await SeedDependenciesAsync();

        var dto = new AddListingDto
        {
            ProductId = product.Id,
            SellerId = seller.Id,
            Price = 199.99m,
            Stock = 50,
            Condition = ListingCondition.New
        };

        var result = await _service.CreateAsync(dto);

        result.Price.Should().Be(199.99m);
        result.Stock.Should().Be(50);
        result.IsActive.Should().BeTrue();
        result.Condition.Should().Be(ListingCondition.New);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidProduct_ShouldThrowBadRequestException()
    {
        var (_, seller) = await SeedDependenciesAsync();

        var dto = new AddListingDto
        {
            ProductId = 999,
            SellerId = seller.Id,
            Price = 100,
            Stock = 10
        };

        var act = () => _service.CreateAsync(dto);
        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Product not found.");
    }

    [Fact]
    public async Task CreateAsync_WithInvalidSeller_ShouldThrowBadRequestException()
    {
        var (product, _) = await SeedDependenciesAsync();

        var dto = new AddListingDto
        {
            ProductId = product.Id,
            SellerId = 999,
            Price = 100,
            Stock = 10
        };

        var act = () => _service.CreateAsync(dto);
        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Seller not found.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task CreateAsync_WithZeroOrNegativePrice_ShouldThrowBadRequestException(decimal price)
    {
        var (product, seller) = await SeedDependenciesAsync();

        var dto = new AddListingDto
        {
            ProductId = product.Id,
            SellerId = seller.Id,
            Price = price,
            Stock = 10
        };

        var act = () => _service.CreateAsync(dto);
        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Price must be > 0.");
    }

    [Fact]
    public async Task CreateAsync_WithNegativeStock_ShouldThrowBadRequestException()
    {
        var (product, seller) = await SeedDependenciesAsync();

        var dto = new AddListingDto
        {
            ProductId = product.Id,
            SellerId = seller.Id,
            Price = 100,
            Stock = -1
        };

        var act = () => _service.CreateAsync(dto);
        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Stock must be >= 0.");
    }

    [Fact]
    public async Task CreateAsync_WithZeroStock_ShouldSucceed()
    {
        var (product, seller) = await SeedDependenciesAsync();

        var dto = new AddListingDto
        {
            ProductId = product.Id,
            SellerId = seller.Id,
            Price = 100,
            Stock = 0
        };

        var result = await _service.CreateAsync(dto);
        result.Stock.Should().Be(0);
    }

    [Theory]
    [InlineData(ListingCondition.New)]
    [InlineData(ListingCondition.LikeNew)]
    [InlineData(ListingCondition.Used)]
    [InlineData(ListingCondition.Refurbished)]
    public async Task CreateAsync_WithDifferentConditions_ShouldSetCorrectly(ListingCondition condition)
    {
        var (product, seller) = await SeedDependenciesAsync();

        var dto = new AddListingDto
        {
            ProductId = product.Id,
            SellerId = seller.Id,
            Price = 100,
            Stock = 10,
            Condition = condition
        };

        var result = await _service.CreateAsync(dto);
        result.Condition.Should().Be(condition);
    }

    #endregion

    #region UpdateAsync

    [Fact]
    public async Task UpdateAsync_WithValidData_ShouldUpdateListing()
    {
        var (product, seller) = await SeedDependenciesAsync();
        var listing = await SeedListingAsync(product.Id, seller.Id, 100, 10);

        var dto = new UpdateListingDto
        {
            Price = 199.99m,
            Stock = 50,
            Condition = ListingCondition.Refurbished,
            IsActive = true
        };

        var result = await _service.UpdateAsync(listing.Id, dto);
        result.Price.Should().Be(199.99m);
        result.Stock.Should().Be(50);
        result.Condition.Should().Be(ListingCondition.Refurbished);
    }

    [Fact]
    public async Task UpdateAsync_WhenNotExists_ShouldThrowNotFoundException()
    {
        var dto = new UpdateListingDto
        {
            Price = 100,
            Stock = 10,
            IsActive = true
        };

        var act = () => _service.UpdateAsync(999, dto);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_WithNullCondition_ShouldKeepExisting()
    {
        var (product, seller) = await SeedDependenciesAsync();
        var listing = await SeedListingAsync(product.Id, seller.Id);

        var dto = new UpdateListingDto
        {
            Price = 200,
            Stock = 5,
            Condition = null,
            IsActive = true
        };

        var result = await _service.UpdateAsync(listing.Id, dto);
        result.Condition.Should().Be(ListingCondition.New);
    }

    [Fact]
    public async Task UpdateAsync_CanDeactivateListing()
    {
        var (product, seller) = await SeedDependenciesAsync();
        var listing = await SeedListingAsync(product.Id, seller.Id);

        var dto = new UpdateListingDto
        {
            Price = 100,
            Stock = 10,
            IsActive = false
        };

        var result = await _service.UpdateAsync(listing.Id, dto);
        result.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_WithZeroPrice_ShouldThrowBadRequestException()
    {
        var (product, seller) = await SeedDependenciesAsync();
        var listing = await SeedListingAsync(product.Id, seller.Id);

        var dto = new UpdateListingDto
        {
            Price = 0,
            Stock = 10,
            IsActive = true
        };

        var act = () => _service.UpdateAsync(listing.Id, dto);
        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Fact]
    public async Task UpdateAsync_WithNegativeStock_ShouldThrowBadRequestException()
    {
        var (product, seller) = await SeedDependenciesAsync();
        var listing = await SeedListingAsync(product.Id, seller.Id);

        var dto = new UpdateListingDto
        {
            Price = 100,
            Stock = -1,
            IsActive = true
        };

        var act = () => _service.UpdateAsync(listing.Id, dto);
        await act.Should().ThrowAsync<BadRequestException>();
    }

    #endregion

    #region SoftDeleteAsync

    [Fact]
    public async Task SoftDeleteAsync_WhenExists_ShouldMarkAsDeleted()
    {
        var (product, seller) = await SeedDependenciesAsync();
        var listing = await SeedListingAsync(product.Id, seller.Id);

        await _service.SoftDeleteAsync(listing.Id);

        var deleted = await _db.Listings.IgnoreQueryFilters()
            .FirstAsync(l => l.Id == listing.Id);
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
