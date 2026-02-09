using Exodus.Data;
using Exodus.Models.Entities;
using Exodus.Services.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Exodus.Controllers.Admin;

[Route("api/admin/banners")]
[ApiController]
[Authorize(Policy = "AdminOnly")]
public class AdminBannerController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public AdminBannerController(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Get all banners
    /// </summary>
    [HttpGet]
    public async Task<ActionResult> GetBanners(
        [FromQuery] BannerPosition? position = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] bool includeExpired = false)
    {
        var query = _db.Banners.AsQueryable();

        if (position.HasValue)
            query = query.Where(b => b.Position == position.Value);

        if (isActive.HasValue)
            query = query.Where(b => b.IsActive == isActive.Value);

        if (!includeExpired)
        {
            var now = DateTime.UtcNow;
            query = query.Where(b =>
                (!b.StartDate.HasValue || b.StartDate <= now) &&
                (!b.EndDate.HasValue || b.EndDate >= now));
        }

        var banners = await query
            .OrderBy(b => b.Position)
            .ThenBy(b => b.DisplayOrder)
            .Select(b => new
            {
                b.Id,
                b.Title,
                b.Description,
                b.ImageUrl,
                b.MobileImageUrl,
                b.TargetUrl,
                b.Position,
                b.DisplayOrder,
                b.IsActive,
                b.StartDate,
                b.EndDate,
                b.ClickCount,
                b.ViewCount,
                IsCurrentlyActive = b.IsActive &&
                    (!b.StartDate.HasValue || b.StartDate <= DateTime.UtcNow) &&
                    (!b.EndDate.HasValue || b.EndDate >= DateTime.UtcNow),
                b.CreatedAt,
                b.UpdatedAt
            })
            .ToListAsync();

        return Ok(banners);
    }

    /// <summary>
    /// Get banner by ID
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetBanner(int id)
    {
        var banner = await _db.Banners.FindAsync(id);
        if (banner == null)
            throw new NotFoundException("Banner not found");

        return Ok(banner);
    }

    /// <summary>
    /// Create new banner
    /// </summary>
    [HttpPost]
    public async Task<ActionResult> CreateBanner([FromBody] CreateBannerDto dto)
    {
        var banner = new Banner
        {
            Title = dto.Title,
            Description = dto.Description,
            ImageUrl = dto.ImageUrl,
            MobileImageUrl = dto.MobileImageUrl,
            TargetUrl = dto.TargetUrl,
            Position = dto.Position,
            DisplayOrder = dto.DisplayOrder,
            IsActive = dto.IsActive,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate
        };

        _db.Banners.Add(banner);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetBanner), new { id = banner.Id }, new { Message = "Banner created", BannerId = banner.Id });
    }

    /// <summary>
    /// Update banner
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult> UpdateBanner(int id, [FromBody] UpdateBannerDto dto)
    {
        var banner = await _db.Banners.FindAsync(id);
        if (banner == null)
            throw new NotFoundException("Banner not found");

        if (!string.IsNullOrEmpty(dto.Title))
            banner.Title = dto.Title;

        if (dto.Description != null)
            banner.Description = dto.Description;

        if (!string.IsNullOrEmpty(dto.ImageUrl))
            banner.ImageUrl = dto.ImageUrl;

        if (dto.MobileImageUrl != null)
            banner.MobileImageUrl = dto.MobileImageUrl;

        if (dto.TargetUrl != null)
            banner.TargetUrl = dto.TargetUrl;

        if (dto.Position.HasValue)
            banner.Position = dto.Position.Value;

        if (dto.DisplayOrder.HasValue)
            banner.DisplayOrder = dto.DisplayOrder.Value;

        if (dto.IsActive.HasValue)
            banner.IsActive = dto.IsActive.Value;

        if (dto.StartDate.HasValue)
            banner.StartDate = dto.StartDate;

        if (dto.EndDate.HasValue)
            banner.EndDate = dto.EndDate;

        await _db.SaveChangesAsync();

        return Ok(new { Message = "Banner updated", BannerId = id });
    }

    /// <summary>
    /// Toggle banner active status
    /// </summary>
    [HttpPatch("{id:int}/toggle-active")]
    public async Task<ActionResult> ToggleActive(int id)
    {
        var banner = await _db.Banners.FindAsync(id);
        if (banner == null)
            throw new NotFoundException("Banner not found");

        banner.IsActive = !banner.IsActive;
        await _db.SaveChangesAsync();

        return Ok(new { Message = banner.IsActive ? "Banner activated" : "Banner deactivated", BannerId = id, IsActive = banner.IsActive });
    }

    /// <summary>
    /// Update banner order
    /// </summary>
    [HttpPatch("{id:int}/order")]
    public async Task<ActionResult> UpdateOrder(int id, [FromBody] UpdateOrderDto dto)
    {
        var banner = await _db.Banners.FindAsync(id);
        if (banner == null)
            throw new NotFoundException("Banner not found");

        banner.DisplayOrder = dto.DisplayOrder;
        await _db.SaveChangesAsync();

        return Ok(new { Message = "Banner order updated", BannerId = id, DisplayOrder = dto.DisplayOrder });
    }

    /// <summary>
    /// Reorder banners for a position
    /// </summary>
    [HttpPost("reorder")]
    public async Task<ActionResult> ReorderBanners([FromBody] ReorderBannersDto dto)
    {
        var banners = await _db.Banners
            .Where(b => dto.BannerOrders.Select(x => x.Id).Contains(b.Id))
            .ToListAsync();

        foreach (var order in dto.BannerOrders)
        {
            var banner = banners.FirstOrDefault(b => b.Id == order.Id);
            if (banner != null)
                banner.DisplayOrder = order.Order;
        }

        await _db.SaveChangesAsync();

        return Ok(new { Message = "Banners reordered" });
    }

    /// <summary>
    /// Delete banner
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeleteBanner(int id)
    {
        var banner = await _db.Banners.FindAsync(id);
        if (banner == null)
            throw new NotFoundException("Banner not found");

        _db.Banners.Remove(banner); // Soft delete
        await _db.SaveChangesAsync();

        return Ok(new { Message = "Banner deleted", BannerId = id });
    }

    /// <summary>
    /// Track banner click (public endpoint)
    /// </summary>
    [HttpPost("{id:int}/click")]
    [AllowAnonymous]
    public async Task<ActionResult> TrackClick(int id)
    {
        var banner = await _db.Banners.FindAsync(id);
        if (banner == null)
            return NotFound();

        banner.ClickCount++;
        await _db.SaveChangesAsync();

        return Ok(new { TargetUrl = banner.TargetUrl });
    }

    /// <summary>
    /// Track banner view (public endpoint)
    /// </summary>
    [HttpPost("{id:int}/view")]
    [AllowAnonymous]
    public async Task<ActionResult> TrackView(int id)
    {
        var banner = await _db.Banners.FindAsync(id);
        if (banner == null)
            return NotFound();

        banner.ViewCount++;
        await _db.SaveChangesAsync();

        return Ok();
    }

    /// <summary>
    /// Get banner statistics
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult> GetStatistics()
    {
        var banners = await _db.Banners.ToListAsync();
        var now = DateTime.UtcNow;

        var stats = new
        {
            TotalBanners = banners.Count,
            ActiveBanners = banners.Count(b => b.IsActive &&
                (!b.StartDate.HasValue || b.StartDate <= now) &&
                (!b.EndDate.HasValue || b.EndDate >= now)),
            TotalClicks = banners.Sum(b => b.ClickCount),
            TotalViews = banners.Sum(b => b.ViewCount),
            AverageClickRate = banners.Where(b => b.ViewCount > 0).Any()
                ? banners.Where(b => b.ViewCount > 0).Average(b => (double)b.ClickCount / b.ViewCount * 100)
                : 0,
            ByPosition = banners
                .GroupBy(b => b.Position)
                .Select(g => new
                {
                    Position = g.Key.ToString(),
                    Count = g.Count(),
                    ActiveCount = g.Count(b => b.IsActive),
                    TotalClicks = g.Sum(b => b.ClickCount)
                })
                .ToList(),
            TopPerforming = banners
                .Where(b => b.ViewCount > 0)
                .OrderByDescending(b => (double)b.ClickCount / b.ViewCount)
                .Take(5)
                .Select(b => new
                {
                    b.Id,
                    b.Title,
                    b.ClickCount,
                    b.ViewCount,
                    ClickRate = (double)b.ClickCount / b.ViewCount * 100
                })
                .ToList()
        };

        return Ok(stats);
    }
}

// Public endpoint for getting active banners
[Route("api/banners")]
[ApiController]
public class BannerController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public BannerController(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Get active banners by position
    /// </summary>
    [HttpGet]
    public async Task<ActionResult> GetActiveBanners([FromQuery] BannerPosition? position = null)
    {
        var now = DateTime.UtcNow;

        var query = _db.Banners
            .Where(b => b.IsActive &&
                (!b.StartDate.HasValue || b.StartDate <= now) &&
                (!b.EndDate.HasValue || b.EndDate >= now));

        if (position.HasValue)
            query = query.Where(b => b.Position == position.Value);

        var banners = await query
            .OrderBy(b => b.Position)
            .ThenBy(b => b.DisplayOrder)
            .Select(b => new
            {
                b.Id,
                b.Title,
                b.Description,
                b.ImageUrl,
                b.MobileImageUrl,
                b.TargetUrl,
                b.Position
            })
            .ToListAsync();

        return Ok(banners);
    }
}

public class CreateBannerDto
{
    [Required]
    [StringLength(200)]
    public required string Title { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    [StringLength(500)]
    public required string ImageUrl { get; set; }

    [StringLength(500)]
    public string? MobileImageUrl { get; set; }

    [StringLength(500)]
    public string? TargetUrl { get; set; }

    public BannerPosition Position { get; set; } = BannerPosition.HomeSlider;

    public int DisplayOrder { get; set; } = 0;

    public bool IsActive { get; set; } = true;

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }
}

public class UpdateBannerDto
{
    [StringLength(200)]
    public string? Title { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    [StringLength(500)]
    public string? ImageUrl { get; set; }

    [StringLength(500)]
    public string? MobileImageUrl { get; set; }

    [StringLength(500)]
    public string? TargetUrl { get; set; }

    public BannerPosition? Position { get; set; }

    public int? DisplayOrder { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }
}

public class UpdateOrderDto
{
    public int DisplayOrder { get; set; }
}

public class ReorderBannersDto
{
    public required List<BannerOrderItem> BannerOrders { get; set; }
}

public class BannerOrderItem
{
    public int Id { get; set; }
    public int Order { get; set; }
}
