using FarmazonDemo.Data;
using FarmazonDemo.Models.Dto;
using FarmazonDemo.Models.Entities;
using FarmazonDemo.Models.Enums;
using FarmazonDemo.Services.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FarmazonDemo.Controllers.Seller
{
    [ApiController]
    [Route("api/seller/listings")]
    [Authorize(Policy = "SellerOnly")]
    public class SellerListingController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SellerListingController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get seller's listings
        /// </summary>
        [HttpGet]
        public async Task<ActionResult> GetMyListings(
            [FromQuery] bool? isActive,
            [FromQuery] StockStatus? stockStatus,
            [FromQuery] string sortBy = "createdAt",
            [FromQuery] string sortOrder = "desc",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var sellerId = GetCurrentUserId();

            var query = _context.Listings
                .Include(l => l.Product)
                    .ThenInclude(p => p.Images)
                .Where(l => l.SellerId == sellerId);

            if (isActive.HasValue)
                query = query.Where(l => l.IsActive == isActive.Value);

            if (stockStatus.HasValue)
                query = query.Where(l => l.StockStatus == stockStatus.Value);

            // Sorting
            query = (sortBy.ToLower(), sortOrder.ToLower()) switch
            {
                ("price", "asc") => query.OrderBy(l => l.Price),
                ("price", "desc") => query.OrderByDescending(l => l.Price),
                ("stock", "asc") => query.OrderBy(l => l.StockQuantity),
                ("stock", "desc") => query.OrderByDescending(l => l.StockQuantity),
                ("createdat", "asc") => query.OrderBy(l => l.CreatedAt),
                _ => query.OrderByDescending(l => l.CreatedAt)
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
                    PrimaryImage = l.Product.Images
                        .Where(i => i.IsPrimary)
                        .Select(i => i.Url)
                        .FirstOrDefault() ?? l.Product.Images.Select(i => i.Url).FirstOrDefault(),
                    l.Price,
                    l.StockQuantity,
                    l.StockStatus,
                    l.LowStockThreshold,
                    l.SKU,
                    l.Condition,
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
        /// Create a new listing
        /// </summary>
        [HttpPost]
        public async Task<ActionResult> CreateListing([FromBody] CreateListingDto dto)
        {
            var sellerId = GetCurrentUserId();

            // Verify product exists
            var productExists = await _context.Products.AnyAsync(p => p.Id == dto.ProductId);
            if (!productExists)
                throw new NotFoundException("Product not found");

            // Check for duplicate listing
            var existingListing = await _context.Listings
                .FirstOrDefaultAsync(l => l.ProductId == dto.ProductId && l.SellerId == sellerId && l.Condition == dto.Condition);

            if (existingListing != null)
                throw new ConflictException("You already have a listing for this product with the same condition");

            var listing = new Listing
            {
                ProductId = dto.ProductId,
                SellerId = sellerId,
                Price = dto.Price,
                StockQuantity = dto.StockQuantity,
                LowStockThreshold = dto.LowStockThreshold ?? 5,
                TrackInventory = dto.TrackInventory ?? true,
                SKU = dto.SKU,
                Condition = dto.Condition,
                IsActive = true,
                StockStatus = dto.StockQuantity > 0 ? StockStatus.InStock : StockStatus.OutOfStock
            };

            _context.Listings.Add(listing);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetMyListings), new { Id = listing.Id });
        }

        /// <summary>
        /// Update listing
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateListing(int id, [FromBody] UpdateListingDto dto)
        {
            var sellerId = GetCurrentUserId();
            var listing = await _context.Listings
                .FirstOrDefaultAsync(l => l.Id == id && l.SellerId == sellerId);

            if (listing == null)
                throw new NotFoundException("Listing not found");

            if (dto.Price.HasValue)
                listing.Price = dto.Price.Value;

            if (dto.StockQuantity.HasValue)
            {
                listing.StockQuantity = dto.StockQuantity.Value;
                UpdateStockStatus(listing);
            }

            if (dto.LowStockThreshold.HasValue)
                listing.LowStockThreshold = dto.LowStockThreshold.Value;

            if (dto.TrackInventory.HasValue)
                listing.TrackInventory = dto.TrackInventory.Value;

            if (dto.SKU != null)
                listing.SKU = dto.SKU;

            if (dto.Condition.HasValue)
                listing.Condition = dto.Condition.Value;

            if (dto.IsActive.HasValue)
                listing.IsActive = dto.IsActive.Value;

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Listing updated" });
        }

        /// <summary>
        /// Update stock quantity
        /// </summary>
        [HttpPatch("{id}/stock")]
        public async Task<ActionResult> UpdateStock(int id, [FromBody] UpdateStockDto dto)
        {
            var sellerId = GetCurrentUserId();
            var listing = await _context.Listings
                .FirstOrDefaultAsync(l => l.Id == id && l.SellerId == sellerId);

            if (listing == null)
                throw new NotFoundException("Listing not found");

            listing.StockQuantity = dto.StockQuantity;
            UpdateStockStatus(listing);

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Stock updated", listing.StockQuantity, listing.StockStatus });
        }

        /// <summary>
        /// Get low stock listings
        /// </summary>
        [HttpGet("low-stock")]
        public async Task<ActionResult> GetLowStock()
        {
            var sellerId = GetCurrentUserId();

            var listings = await _context.Listings
                .Include(l => l.Product)
                .Where(l => l.SellerId == sellerId &&
                           l.TrackInventory &&
                           l.StockQuantity > 0 &&
                           l.StockQuantity <= l.LowStockThreshold)
                .Select(l => new
                {
                    l.Id,
                    l.ProductId,
                    ProductName = l.Product.ProductName,
                    l.StockQuantity,
                    l.LowStockThreshold,
                    l.SKU
                })
                .ToListAsync();

            return Ok(listings);
        }

        /// <summary>
        /// Get out of stock listings
        /// </summary>
        [HttpGet("out-of-stock")]
        public async Task<ActionResult> GetOutOfStock()
        {
            var sellerId = GetCurrentUserId();

            var listings = await _context.Listings
                .Include(l => l.Product)
                .Where(l => l.SellerId == sellerId && l.StockQuantity == 0)
                .Select(l => new
                {
                    l.Id,
                    l.ProductId,
                    ProductName = l.Product.ProductName,
                    l.SKU,
                    l.IsActive
                })
                .ToListAsync();

            return Ok(listings);
        }

        /// <summary>
        /// Delete listing
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteListing(int id)
        {
            var sellerId = GetCurrentUserId();
            var listing = await _context.Listings
                .FirstOrDefaultAsync(l => l.Id == id && l.SellerId == sellerId);

            if (listing == null)
                throw new NotFoundException("Listing not found");

            _context.Listings.Remove(listing);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private void UpdateStockStatus(Listing listing)
        {
            if (listing.StockQuantity == 0)
                listing.StockStatus = StockStatus.OutOfStock;
            else if (listing.StockQuantity <= listing.LowStockThreshold)
                listing.StockStatus = StockStatus.LowStock;
            else
                listing.StockStatus = StockStatus.InStock;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                throw new UnauthorizedException("Invalid user token");
            return userId;
        }
    }

    public class CreateListingDto
    {
        public int ProductId { get; set; }
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public int? LowStockThreshold { get; set; }
        public bool? TrackInventory { get; set; }
        public string? SKU { get; set; }
        public ListingCondition Condition { get; set; } = ListingCondition.New;
    }

    public class UpdateListingDto
    {
        public decimal? Price { get; set; }
        public int? StockQuantity { get; set; }
        public int? LowStockThreshold { get; set; }
        public bool? TrackInventory { get; set; }
        public string? SKU { get; set; }
        public ListingCondition? Condition { get; set; }
        public bool? IsActive { get; set; }
    }

    public class UpdateStockDto
    {
        public int StockQuantity { get; set; }
    }
}
