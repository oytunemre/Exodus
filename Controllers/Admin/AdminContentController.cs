using Exodus.Data;
using Exodus.Models.Entities;
using Exodus.Services.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace Exodus.Controllers.Admin;

[Route("api/admin/content")]
[ApiController]
[Authorize(Policy = "AdminOnly")]
public class AdminContentController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public AdminContentController(ApplicationDbContext db)
    {
        _db = db;
    }

    #region Static Pages

    /// <summary>
    /// Get all static pages
    /// </summary>
    [HttpGet("pages")]
    public async Task<ActionResult> GetPages(
        [FromQuery] StaticPageType? type = null,
        [FromQuery] bool? isPublished = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _db.StaticPages.AsQueryable();

        if (type.HasValue)
            query = query.Where(p => p.PageType == type.Value);

        if (isPublished.HasValue)
            query = query.Where(p => p.IsPublished == isPublished.Value);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(p => p.Title.Contains(search) || p.Slug.Contains(search));

        var totalCount = await query.CountAsync();

        var pages = await query
            .OrderBy(p => p.DisplayOrder)
            .ThenBy(p => p.Title)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new
            {
                p.Id,
                p.Title,
                p.Slug,
                p.PageType,
                p.IsPublished,
                p.ShowInFooter,
                p.ShowInHeader,
                p.DisplayOrder,
                p.PublishedAt,
                p.CreatedAt,
                p.UpdatedAt
            })
            .ToListAsync();

        return Ok(new
        {
            Items = pages,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
    }

    /// <summary>
    /// Get page by ID
    /// </summary>
    [HttpGet("pages/{id:int}")]
    public async Task<ActionResult> GetPage(int id)
    {
        var pagee = await _db.StaticPages.FindAsync(id);
        if (pagee == null)
            throw new NotFoundException("Page not found");

        return Ok(pagee);
    }

    /// <summary>
    /// Get page by slug
    /// </summary>
    [HttpGet("pages/by-slug/{slug}")]
    public async Task<ActionResult> GetPageBySlug(string slug)
    {
        var pagee = await _db.StaticPages.FirstOrDefaultAsync(p => p.Slug == slug);
        if (pagee == null)
            throw new NotFoundException("Page not found");

        return Ok(pagee);
    }

    /// <summary>
    /// Create static page
    /// </summary>
    [HttpPost("pages")]
    public async Task<ActionResult> CreatePage([FromBody] CreatePageDto dto)
    {
        var adminId = GetCurrentUserId();

        // Generate slug if not provided
        var slug = !string.IsNullOrEmpty(dto.Slug)
            ? dto.Slug.ToLower()
            : GenerateSlug(dto.Title);

        // Check slug uniqueness
        var slugExists = await _db.StaticPages.AnyAsync(p => p.Slug == slug);
        if (slugExists)
            throw new BadRequestException("Slug already exists");

        var pagee = new StaticPage
        {
            Title = dto.Title,
            Slug = slug,
            Content = dto.Content,
            MetaTitle = dto.MetaTitle ?? dto.Title,
            MetaDescription = dto.MetaDescription,
            MetaKeywords = dto.MetaKeywords,
            IsPublished = dto.IsPublished,
            ShowInFooter = dto.ShowInFooter,
            ShowInHeader = dto.ShowInHeader,
            DisplayOrder = dto.DisplayOrder,
            PageType = dto.PageType,
            PublishedAt = dto.IsPublished ? DateTime.UtcNow : null,
            LastEditedByUserId = adminId
        };

        _db.StaticPages.Add(pagee);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetPage), new { id = pagee.Id }, new
        {
            Message = "Page created successfully",
            PageId = pagee.Id,
            Slug = pagee.Slug
        });
    }

    /// <summary>
    /// Update static page
    /// </summary>
    [HttpPut("pages/{id:int}")]
    public async Task<ActionResult> UpdatePage(int id, [FromBody] UpdatePageDto dto)
    {
        var adminId = GetCurrentUserId();

        var pagee = await _db.StaticPages.FindAsync(id);
        if (pagee == null)
            throw new NotFoundException("Page not found");

        // Check slug uniqueness if changed
        if (!string.IsNullOrEmpty(dto.Slug) && dto.Slug != pagee.Slug)
        {
            var slugExists = await _db.StaticPages.AnyAsync(p => p.Slug == dto.Slug && p.Id != id);
            if (slugExists)
                throw new BadRequestException("Slug already exists");
            pagee.Slug = dto.Slug.ToLower();
        }

        if (!string.IsNullOrEmpty(dto.Title))
            pagee.Title = dto.Title;

        if (dto.Content != null)
            pagee.Content = dto.Content;

        if (dto.MetaTitle != null)
            pagee.MetaTitle = dto.MetaTitle;

        if (dto.MetaDescription != null)
            pagee.MetaDescription = dto.MetaDescription;

        if (dto.MetaKeywords != null)
            pagee.MetaKeywords = dto.MetaKeywords;

        if (dto.IsPublished.HasValue)
        {
            var wasPublished = pagee.IsPublished;
            pagee.IsPublished = dto.IsPublished.Value;
            if (dto.IsPublished.Value && !wasPublished)
                pagee.PublishedAt = DateTime.UtcNow;
        }

        if (dto.ShowInFooter.HasValue)
            pagee.ShowInFooter = dto.ShowInFooter.Value;

        if (dto.ShowInHeader.HasValue)
            pagee.ShowInHeader = dto.ShowInHeader.Value;

        if (dto.DisplayOrder.HasValue)
            pagee.DisplayOrder = dto.DisplayOrder.Value;

        if (dto.PageType.HasValue)
            pagee.PageType = dto.PageType.Value;

        pagee.LastEditedByUserId = adminId;

        await _db.SaveChangesAsync();

        return Ok(new { Message = "Page updated successfully", PageId = id });
    }

    /// <summary>
    /// Delete static page
    /// </summary>
    [HttpDelete("pages/{id:int}")]
    public async Task<ActionResult> DeletePage(int id)
    {
        var pagee = await _db.StaticPages.FindAsync(id);
        if (pagee == null)
            throw new NotFoundException("Page not found");

        _db.StaticPages.Remove(pagee);
        await _db.SaveChangesAsync();

        return Ok(new { Message = "Page deleted", PageId = id });
    }

    /// <summary>
    /// Toggle page publish status
    /// </summary>
    [HttpPatch("pages/{id:int}/toggle-publish")]
    public async Task<ActionResult> TogglePublish(int id)
    {
        var pagee = await _db.StaticPages.FindAsync(id);
        if (pagee == null)
            throw new NotFoundException("Page not found");

        pagee.IsPublished = !pagee.IsPublished;
        if (pagee.IsPublished && !pagee.PublishedAt.HasValue)
            pagee.PublishedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(new
        {
            Message = pagee.IsPublished ? "Page published" : "Page unpublished",
            PageId = id,
            IsPublished = pagee.IsPublished
        });
    }

    /// <summary>
    /// Reorder pages
    /// </summary>
    [HttpPost("pages/reorder")]
    public async Task<ActionResult> ReorderPages([FromBody] ReorderPagesDto dto)
    {
        foreach (var item in dto.Items)
        {
            var pagee = await _db.StaticPages.FindAsync(item.PageId);
            if (pagee != null)
                pagee.DisplayOrder = item.DisplayOrder;
        }

        await _db.SaveChangesAsync();

        return Ok(new { Message = "Pages reordered successfully" });
    }

    #endregion

    #region SEO Settings

    /// <summary>
    /// Get SEO settings
    /// </summary>
    [HttpGet("seo")]
    public async Task<ActionResult> GetSeoSettings()
    {
        var seoSettings = await _db.SiteSettings
            .Where(s => s.Category == SettingCategory.Seo)
            .ToDictionaryAsync(s => s.Key, s => s.Value);

        return Ok(seoSettings);
    }

    /// <summary>
    /// Update SEO settings
    /// </summary>
    [HttpPut("seo")]
    public async Task<ActionResult> UpdateSeoSettings([FromBody] Dictionary<string, string> settings)
    {
        foreach (var kvp in settings)
        {
            var setting = await _db.SiteSettings
                .FirstOrDefaultAsync(s => s.Key == kvp.Key && s.Category == SettingCategory.Seo);

            if (setting != null)
            {
                setting.Value = kvp.Value;
            }
            else
            {
                _db.SiteSettings.Add(new SiteSetting
                {
                    Key = kvp.Key,
                    Value = kvp.Value,
                    Category = SettingCategory.Seo,
                    IsPublic = true
                });
            }
        }

        await _db.SaveChangesAsync();

        return Ok(new { Message = "SEO settings updated" });
    }

    #endregion

    #region Menu Management

    /// <summary>
    /// Get header menu items
    /// </summary>
    [HttpGet("menu/header")]
    public async Task<ActionResult> GetHeaderMenu()
    {
        var pages = await _db.StaticPages
            .Where(p => p.ShowInHeader && p.IsPublished)
            .OrderBy(p => p.DisplayOrder)
            .Select(p => new
            {
                p.Id,
                p.Title,
                p.Slug,
                Url = $"/page/{p.Slug}"
            })
            .ToListAsync();

        return Ok(pages);
    }

    /// <summary>
    /// Get footer menu items
    /// </summary>
    [HttpGet("menu/footer")]
    public async Task<ActionResult> GetFooterMenu()
    {
        var pages = await _db.StaticPages
            .Where(p => p.ShowInFooter && p.IsPublished)
            .OrderBy(p => p.DisplayOrder)
            .Select(p => new
            {
                p.Id,
                p.Title,
                p.Slug,
                Url = $"/page/{p.Slug}"
            })
            .ToListAsync();

        return Ok(pages);
    }

    #endregion

    private static string GenerateSlug(string title)
    {
        var slug = title.ToLower();
        // Replace Turkish characters
        slug = slug.Replace("ı", "i").Replace("ğ", "g").Replace("ü", "u")
                   .Replace("ş", "s").Replace("ö", "o").Replace("ç", "c");
        // Replace spaces with hyphens
        slug = Regex.Replace(slug, @"\s+", "-");
        // Remove invalid characters
        slug = Regex.Replace(slug, @"[^a-z0-9\-]", "");
        // Remove multiple hyphens
        slug = Regex.Replace(slug, @"-+", "-");
        // Trim hyphens from start/end
        slug = slug.Trim('-');
        return slug;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedException("Invalid user token");
        return userId;
    }
}

/// <summary>
/// Public controller for reading static pages
/// </summary>
[Route("api/pages")]
[ApiController]
public class PageController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public PageController(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Get published page by slug
    /// </summary>
    [HttpGet("{slug}")]
    public async Task<ActionResult> GetPage(string slug)
    {
        var pagee = await _db.StaticPages
            .Where(p => p.Slug == slug && p.IsPublished)
            .Select(p => new
            {
                p.Id,
                p.Title,
                p.Slug,
                p.Content,
                p.MetaTitle,
                p.MetaDescription,
                p.MetaKeywords,
                p.PageType,
                p.PublishedAt,
                p.UpdatedAt
            })
            .FirstOrDefaultAsync();

        if (pagee == null)
            throw new NotFoundException("Page not found");

        return Ok(pagee);
    }

    /// <summary>
    /// Get header navigation pages
    /// </summary>
    [HttpGet("navigation/header")]
    public async Task<ActionResult> GetHeaderNavigation()
    {
        var pages = await _db.StaticPages
            .Where(p => p.ShowInHeader && p.IsPublished)
            .OrderBy(p => p.DisplayOrder)
            .Select(p => new { p.Title, p.Slug })
            .ToListAsync();

        return Ok(pages);
    }

    /// <summary>
    /// Get footer navigation pages
    /// </summary>
    [HttpGet("navigation/footer")]
    public async Task<ActionResult> GetFooterNavigation()
    {
        var pages = await _db.StaticPages
            .Where(p => p.ShowInFooter && p.IsPublished)
            .OrderBy(p => p.DisplayOrder)
            .Select(p => new { p.Title, p.Slug })
            .ToListAsync();

        return Ok(pages);
    }
}

public class CreatePageDto
{
    [Required]
    [StringLength(200)]
    public required string Title { get; set; }

    [StringLength(200)]
    public string? Slug { get; set; }

    [Required]
    public required string Content { get; set; }

    [StringLength(200)]
    public string? MetaTitle { get; set; }

    [StringLength(500)]
    public string? MetaDescription { get; set; }

    [StringLength(500)]
    public string? MetaKeywords { get; set; }

    public bool IsPublished { get; set; } = false;
    public bool ShowInFooter { get; set; } = false;
    public bool ShowInHeader { get; set; } = false;
    public int DisplayOrder { get; set; } = 0;

    public StaticPageType PageType { get; set; } = StaticPageType.General;
}

public class UpdatePageDto
{
    [StringLength(200)]
    public string? Title { get; set; }

    [StringLength(200)]
    public string? Slug { get; set; }

    public string? Content { get; set; }

    [StringLength(200)]
    public string? MetaTitle { get; set; }

    [StringLength(500)]
    public string? MetaDescription { get; set; }

    [StringLength(500)]
    public string? MetaKeywords { get; set; }

    public bool? IsPublished { get; set; }
    public bool? ShowInFooter { get; set; }
    public bool? ShowInHeader { get; set; }
    public int? DisplayOrder { get; set; }

    public StaticPageType? PageType { get; set; }
}

public class ReorderPagesDto
{
    public required List<PageOrderItem> Items { get; set; }
}

public class PageOrderItem
{
    public int PageId { get; set; }
    public int DisplayOrder { get; set; }
}
