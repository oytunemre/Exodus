using FarmazonDemo.Data;
using FarmazonDemo.Models.Entities;
using FarmazonDemo.Services.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FarmazonDemo.Controllers.Admin;

[Route("api/admin/wishlists")]
[ApiController]
[Authorize(Policy = "AdminOnly")]
public class AdminWishlistController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public AdminWishlistController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult> GetWishlists(
        [FromQuery] int? userId = null,
        [FromQuery] bool? isPublic = null,
        [FromQuery] string? sortBy = "createdAt",
        [FromQuery] bool sortDesc = true,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _db.Set<Wishlist>()
            .Include(w => w.User)
            .Include(w => w.Items)
            .AsQueryable();

        if (userId.HasValue)
            query = query.Where(w => w.UserId == userId.Value);

        if (isPublic.HasValue)
            query = query.Where(w => w.IsPublic == isPublic.Value);

        query = sortBy?.ToLower() switch
        {
            "name" => sortDesc ? query.OrderByDescending(w => w.Name) : query.OrderBy(w => w.Name),
            "itemcount" => sortDesc ? query.OrderByDescending(w => w.Items.Count) : query.OrderBy(w => w.Items.Count),
            _ => sortDesc ? query.OrderByDescending(w => w.CreatedAt) : query.OrderBy(w => w.CreatedAt)
        };

        var totalCount = await query.CountAsync();

        var wishlists = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(w => new
            {
                w.Id,
                w.Name,
                w.UserId,
                UserEmail = w.User.Email,
                UserName = w.User.Name,
                w.IsDefault,
                w.IsPublic,
                ItemCount = w.Items.Count,
                w.CreatedAt,
                w.UpdatedAt
            })
            .ToListAsync();

        return Ok(new
        {
            Items = wishlists,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetWishlist(int id)
    {
        var wishlist = await _db.Set<Wishlist>()
            .Include(w => w.User)
            .Include(w => w.Items)
                .ThenInclude(i => i.Product)
            .Include(w => w.Items)
                .ThenInclude(i => i.Listing)
            .FirstOrDefaultAsync(w => w.Id == id);

        if (wishlist == null) throw new NotFoundException("Wishlist not found");

        return Ok(new
        {
            wishlist.Id,
            wishlist.Name,
            wishlist.UserId,
            UserEmail = wishlist.User.Email,
            UserName = wishlist.User.Name,
            wishlist.IsDefault,
            wishlist.IsPublic,
            Items = wishlist.Items.Select(i => new
            {
                i.Id,
                i.ProductId,
                ProductName = i.Product.ProductName,
                ProductImage = i.Product.Images.Where(img => img.IsPrimary).Select(img => img.Url).FirstOrDefault(),
                i.ListingId,
                ListingPrice = i.Listing?.Price,
                i.Note,
                i.NotifyOnPriceDrop,
                i.PriceAtAdd,
                i.CreatedAt
            }),
            wishlist.CreatedAt,
            wishlist.UpdatedAt
        });
    }

    [HttpGet("user/{userId:int}")]
    public async Task<ActionResult> GetUserWishlists(int userId)
    {
        var wishlists = await _db.Set<Wishlist>()
            .Include(w => w.Items)
            .Where(w => w.UserId == userId)
            .Select(w => new
            {
                w.Id,
                w.Name,
                w.IsDefault,
                w.IsPublic,
                ItemCount = w.Items.Count,
                w.CreatedAt
            })
            .ToListAsync();

        return Ok(wishlists);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeleteWishlist(int id)
    {
        var wishlist = await _db.Set<Wishlist>().FindAsync(id);
        if (wishlist == null) throw new NotFoundException("Wishlist not found");

        _db.Remove(wishlist);
        await _db.SaveChangesAsync();

        return Ok(new { Message = "Wishlist deleted" });
    }

    [HttpDelete("items/{itemId:int}")]
    public async Task<ActionResult> DeleteWishlistItem(int itemId)
    {
        var item = await _db.Set<WishlistItem>().FindAsync(itemId);
        if (item == null) throw new NotFoundException("Item not found");

        _db.Remove(item);
        await _db.SaveChangesAsync();

        return Ok(new { Message = "Item removed from wishlist" });
    }

    [HttpGet("statistics")]
    public async Task<ActionResult> GetStatistics()
    {
        var wishlists = await _db.Set<Wishlist>().Include(w => w.Items).ToListAsync();
        var items = wishlists.SelectMany(w => w.Items).ToList();

        var stats = new
        {
            TotalWishlists = wishlists.Count,
            PublicWishlists = wishlists.Count(w => w.IsPublic),
            TotalItems = items.Count,
            UniqueUsers = wishlists.Select(w => w.UserId).Distinct().Count(),
            ItemsWithPriceAlert = items.Count(i => i.NotifyOnPriceDrop),

            TopProducts = items
                .GroupBy(i => i.ProductId)
                .Select(g => new { ProductId = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToList(),

            AverageItemsPerWishlist = wishlists.Count > 0
                ? Math.Round(items.Count / (double)wishlists.Count, 2)
                : 0
        };

        return Ok(stats);
    }

    [HttpGet("popular-products")]
    public async Task<ActionResult> GetPopularWishlistProducts([FromQuery] int limit = 20)
    {
        var popularProducts = await _db.Set<WishlistItem>()
            .Include(i => i.Product)
            .GroupBy(i => i.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                ProductName = g.First().Product.ProductName,
                ProductImage = g.First().Product.Images.Where(img => img.IsPrimary).Select(img => img.Url).FirstOrDefault(),
                WishlistCount = g.Count()
            })
            .OrderByDescending(x => x.WishlistCount)
            .Take(limit)
            .ToListAsync();

        return Ok(popularProducts);
    }
}
