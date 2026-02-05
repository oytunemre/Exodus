using FarmazonDemo.Data;
using FarmazonDemo.Models.Enums;
using FarmazonDemo.Services.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FarmazonDemo.Controllers.Admin;

[Route("api/admin/listings")]
[ApiController]
[Authorize(Policy = "AdminOnly")]
public class AdminListingController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public AdminListingController(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Get all listings with filtering and pagination
    /// </summary>
    [HttpGet]
    public async Task<ActionResult> GetListings(
        [FromQuery] int? sellerId = null,
        [FromQuery] int? productId = null,
        [FromQuery] int? categoryId = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] ListingCondition? condition = null,
        [FromQuery] StockStatus? stockStatus = null,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = "createdAt",
        [FromQuery] bool sortDesc = true,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _db.Listings
            .Include(l => l.Product)
                .ThenInclude(p => p.Images)
            .Include(l => l.Product)
                .ThenInclude(p => p.Category)
            .Include(l => l.Seller)
            .AsQueryable();

        // Filters
        if (sellerId.HasValue)
            query = query.Where(l => l.SellerId == sellerId.Value);

        if (productId.HasValue)
            query = query.Where(l => l.ProductId == productId.Value);

        if (categoryId.HasValue)
            query = query.Where(l => l.Product.CategoryId == categoryId.Value);

        if (isActive.HasValue)
            query = query.Where(l => l.IsActive == isActive.Value);

        if (condition.HasValue)
            query = query.Where(l => l.Condition == condition.Value);

        if (stockStatus.HasValue)
            query = query.Where(l => l.StockStatus == stockStatus.Value);

        if (minPrice.HasValue)
            query = query.Where(l => l.Price >= minPrice.Value);

        if (maxPrice.HasValue)
            query = query.Where(l => l.Price <= maxPrice.Value);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(l =>
                l.Product.ProductName.Contains(search) ||
                l.SKU.Contains(search) ||
                l.Seller.Name.Contains(search));

        // Sorting
        query = sortBy?.ToLower() switch
        {
            "price" => sortDesc ? query.OrderByDescending(l => l.Price) : query.OrderBy(l => l.Price),
            "stock" => sortDesc ? query.OrderByDescending(l => l.StockQuantity) : query.OrderBy(l => l.StockQuantity),
            "seller" => sortDesc ? query.OrderByDescending(l => l.Seller.Name) : query.OrderBy(l => l.Seller.Name),
            "product" => sortDesc ? query.OrderByDescending(l => l.Product.ProductName) : query.OrderBy(l => l.Product.ProductName),
            _ => sortDesc ? query.OrderByDescending(l => l.CreatedAt) : query.OrderBy(l => l.CreatedAt)
        };

        var totalCount = await query.CountAsync();

        var listings = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new
            {
                l.Id,
                l.ProductId,
                ProductName = l.Product.ProductName,
                ProductImage = l.Product.Images.OrderBy(i => i.DisplayOrder).Select(i => i.Url).FirstOrDefault(),
                CategoryName = l.Product.Category != null ? l.Product.Category.Name : null,
                l.SellerId,
                SellerName = l.Seller.Name,
                l.Price,
                l.StockQuantity,
                l.StockStatus,
                l.Condition,
                l.SKU,
                l.IsActive,
                l.CreatedAt,
                l.UpdatedAt
            })
            .ToListAsync();

        return Ok(new
        {
            Items = listings,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        });
    }

    /// <summary>
    /// Get listing details by ID
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetListing(int id)
    {
        var listing = await _db.Listings
            .Include(l => l.Product)
                .ThenInclude(p => p.Images)
            .Include(l => l.Product)
                .ThenInclude(p => p.Category)
            .Include(l => l.Product)
                .ThenInclude(p => p.Barcodes)
            .Include(l => l.Seller)
            .Where(l => l.Id == id)
            .Select(l => new
            {
                l.Id,
                l.ProductId,
                Product = new
                {
                    l.Product.Id,
                    l.Product.ProductName,
                    l.Product.ProductDescription,
                    l.Product.Brand,
                    l.Product.Manufacturer,
                    CategoryName = l.Product.Category != null ? l.Product.Category.Name : null,
                    Images = l.Product.Images.OrderBy(i => i.DisplayOrder).Select(i => new { i.Id, i.Url, i.AltText }),
                    Barcodes = l.Product.Barcodes.Select(b => b.Barcode)
                },
                l.SellerId,
                Seller = new
                {
                    l.Seller.Id,
                    l.Seller.Name,
                    l.Seller.Email,
                    l.Seller.Phone
                },
                l.Price,
                l.StockQuantity,
                l.StockStatus,
                l.Condition,
                l.SKU,
                l.IsActive,
                l.CreatedAt,
                l.UpdatedAt,
                // Stats
                OrderCount = _db.SellerOrderItems.Count(oi => oi.ListingId == l.Id),
                TotalSold = _db.SellerOrderItems.Where(oi => oi.ListingId == l.Id).Sum(oi => (int?)oi.Quantity) ?? 0,
                TotalRevenue = _db.SellerOrderItems.Where(oi => oi.ListingId == l.Id).Sum(oi => (decimal?)oi.LineTotal) ?? 0
            })
            .FirstOrDefaultAsync();

        if (listing == null)
            throw new NotFoundException("Listing not found");

        return Ok(listing);
    }

    /// <summary>
    /// Update listing
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult> UpdateListing(int id, [FromBody] AdminUpdateListingDto dto)
    {
        var listing = await _db.Listings.FindAsync(id);
        if (listing == null)
            throw new NotFoundException("Listing not found");

        if (dto.Price.HasValue)
        {
            if (dto.Price <= 0)
                throw new BadRequestException("Price must be greater than 0");
            listing.Price = dto.Price.Value;
        }

        if (dto.StockQuantity.HasValue)
        {
            if (dto.StockQuantity < 0)
                throw new BadRequestException("Stock cannot be negative");
            listing.StockQuantity = dto.StockQuantity.Value;

            // Update stock status
            listing.StockStatus = listing.StockQuantity switch
            {
                0 => StockStatus.OutOfStock,
                < 10 => StockStatus.LowStock,
                _ => StockStatus.InStock
            };
        }

        if (dto.IsActive.HasValue)
            listing.IsActive = dto.IsActive.Value;

        if (dto.Condition.HasValue)
            listing.Condition = dto.Condition.Value;

        if (!string.IsNullOrEmpty(dto.SKU))
            listing.SKU = dto.SKU;

        await _db.SaveChangesAsync();

        return Ok(new { Message = "Listing updated successfully", ListingId = id });
    }

    /// <summary>
    /// Toggle listing active status
    /// </summary>
    [HttpPatch("{id:int}/toggle-active")]
    public async Task<ActionResult> ToggleActive(int id)
    {
        var listing = await _db.Listings.FindAsync(id);
        if (listing == null)
            throw new NotFoundException("Listing not found");

        listing.IsActive = !listing.IsActive;
        await _db.SaveChangesAsync();

        return Ok(new { Message = listing.IsActive ? "Listing activated" : "Listing deactivated", ListingId = id, IsActive = listing.IsActive });
    }

    /// <summary>
    /// Delete listing (soft delete)
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeleteListing(int id)
    {
        var listing = await _db.Listings.FindAsync(id);
        if (listing == null)
            throw new NotFoundException("Listing not found");

        _db.Listings.Remove(listing); // Soft delete
        await _db.SaveChangesAsync();

        return Ok(new { Message = "Listing deleted successfully", ListingId = id });
    }

    /// <summary>
    /// Bulk update listings (activate/deactivate)
    /// </summary>
    [HttpPost("bulk-update")]
    public async Task<ActionResult> BulkUpdate([FromBody] BulkUpdateListingsDto dto)
    {
        var listings = await _db.Listings
            .Where(l => dto.ListingIds.Contains(l.Id))
            .ToListAsync();

        if (!listings.Any())
            throw new NotFoundException("No listings found");

        foreach (var listing in listings)
        {
            if (dto.IsActive.HasValue)
                listing.IsActive = dto.IsActive.Value;
        }

        await _db.SaveChangesAsync();

        return Ok(new { Message = $"{listings.Count} listings updated", UpdatedIds = listings.Select(l => l.Id) });
    }

    /// <summary>
    /// Get listing statistics
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult> GetStatistics()
    {
        var stats = new
        {
            TotalListings = await _db.Listings.CountAsync(),
            ActiveListings = await _db.Listings.CountAsync(l => l.IsActive),
            InactiveListings = await _db.Listings.CountAsync(l => !l.IsActive),
            InStock = await _db.Listings.CountAsync(l => l.StockStatus == StockStatus.InStock),
            LowStock = await _db.Listings.CountAsync(l => l.StockStatus == StockStatus.LowStock),
            OutOfStock = await _db.Listings.CountAsync(l => l.StockStatus == StockStatus.OutOfStock),
            AveragePrice = await _db.Listings.AverageAsync(l => (decimal?)l.Price) ?? 0,
            TotalStockValue = await _db.Listings.SumAsync(l => (decimal?)(l.Price * l.StockQuantity)) ?? 0,
            ByCondition = await _db.Listings
                .GroupBy(l => l.Condition)
                .Select(g => new { Condition = g.Key.ToString(), Count = g.Count() })
                .ToListAsync()
        };

        return Ok(stats);
    }

    /// <summary>
    /// Get low stock alerts
    /// </summary>
    [HttpGet("low-stock")]
    public async Task<ActionResult> GetLowStockListings([FromQuery] int threshold = 10, [FromQuery] int limit = 50)
    {
        var listings = await _db.Listings
            .Include(l => l.Product)
            .Include(l => l.Seller)
            .Where(l => l.StockQuantity > 0 && l.StockQuantity < threshold && l.IsActive)
            .OrderBy(l => l.StockQuantity)
            .Take(limit)
            .Select(l => new
            {
                l.Id,
                ProductName = l.Product.ProductName,
                SellerName = l.Seller.Name,
                l.SKU,
                l.StockQuantity,
                l.Price
            })
            .ToListAsync();

        return Ok(listings);
    }
}

public class AdminUpdateListingDto
{
    public decimal? Price { get; set; }
    public int? StockQuantity { get; set; }
    public bool? IsActive { get; set; }
    public ListingCondition? Condition { get; set; }
    public string? SKU { get; set; }
}

public class BulkUpdateListingsDto
{
    public required List<int> ListingIds { get; set; }
    public bool? IsActive { get; set; }
}
