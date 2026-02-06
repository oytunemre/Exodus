using Exodus.Data;
using Exodus.Models.Entities;
using Exodus.Services.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Exodus.Controllers.Admin;

[Route("api/admin/brands")]
[ApiController]
[Authorize(Policy = "AdminOnly")]
public class AdminBrandController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public AdminBrandController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult> GetBrands(
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] bool? isFeatured = null,
        [FromQuery] string? sortBy = "displayOrder",
        [FromQuery] bool sortDesc = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _db.Set<Brand>().AsQueryable();

        if (!string.IsNullOrEmpty(search))
            query = query.Where(b => b.Name.Contains(search) || (b.Slug != null && b.Slug.Contains(search)));

        if (isActive.HasValue)
            query = query.Where(b => b.IsActive == isActive.Value);

        if (isFeatured.HasValue)
            query = query.Where(b => b.IsFeatured == isFeatured.Value);

        query = sortBy?.ToLower() switch
        {
            "name" => sortDesc ? query.OrderByDescending(b => b.Name) : query.OrderBy(b => b.Name),
            "createdat" => sortDesc ? query.OrderByDescending(b => b.CreatedAt) : query.OrderBy(b => b.CreatedAt),
            _ => sortDesc ? query.OrderByDescending(b => b.DisplayOrder) : query.OrderBy(b => b.DisplayOrder)
        };

        var totalCount = await query.CountAsync();

        var brands = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new
        {
            Items = brands,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetBrand(int id)
    {
        var brand = await _db.Set<Brand>().FindAsync(id);
        if (brand == null) throw new NotFoundException("Brand not found");
        return Ok(brand);
    }

    [HttpGet("slug/{slug}")]
    public async Task<ActionResult> GetBrandBySlug(string slug)
    {
        var brand = await _db.Set<Brand>().FirstOrDefaultAsync(b => b.Slug == slug);
        if (brand == null) throw new NotFoundException("Brand not found");
        return Ok(brand);
    }

    [HttpPost]
    public async Task<ActionResult> CreateBrand([FromBody] CreateBrandDto dto)
    {
        var slug = string.IsNullOrEmpty(dto.Slug) ? GenerateSlug(dto.Name) : dto.Slug;

        if (await _db.Set<Brand>().AnyAsync(b => b.Slug == slug))
            throw new ValidationException("Slug already exists");

        var brand = new Brand
        {
            Name = dto.Name,
            Slug = slug,
            Description = dto.Description,
            LogoUrl = dto.LogoUrl,
            BannerUrl = dto.BannerUrl,
            Website = dto.Website,
            IsActive = dto.IsActive,
            IsFeatured = dto.IsFeatured,
            DisplayOrder = dto.DisplayOrder,
            MetaTitle = dto.MetaTitle,
            MetaDescription = dto.MetaDescription
        };

        _db.Set<Brand>().Add(brand);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetBrand), new { id = brand.Id }, brand);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> UpdateBrand(int id, [FromBody] UpdateBrandDto dto)
    {
        var brand = await _db.Set<Brand>().FindAsync(id);
        if (brand == null) throw new NotFoundException("Brand not found");

        if (!string.IsNullOrEmpty(dto.Name)) brand.Name = dto.Name;
        if (dto.Slug != null)
        {
            if (await _db.Set<Brand>().AnyAsync(b => b.Slug == dto.Slug && b.Id != id))
                throw new ValidationException("Slug already exists");
            brand.Slug = dto.Slug;
        }
        if (dto.Description != null) brand.Description = dto.Description;
        if (dto.LogoUrl != null) brand.LogoUrl = dto.LogoUrl;
        if (dto.BannerUrl != null) brand.BannerUrl = dto.BannerUrl;
        if (dto.Website != null) brand.Website = dto.Website;
        if (dto.IsActive.HasValue) brand.IsActive = dto.IsActive.Value;
        if (dto.IsFeatured.HasValue) brand.IsFeatured = dto.IsFeatured.Value;
        if (dto.DisplayOrder.HasValue) brand.DisplayOrder = dto.DisplayOrder.Value;
        if (dto.MetaTitle != null) brand.MetaTitle = dto.MetaTitle;
        if (dto.MetaDescription != null) brand.MetaDescription = dto.MetaDescription;

        await _db.SaveChangesAsync();
        return Ok(brand);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeleteBrand(int id)
    {
        var brand = await _db.Set<Brand>().FindAsync(id);
        if (brand == null) throw new NotFoundException("Brand not found");

        _db.Remove(brand);
        await _db.SaveChangesAsync();

        return Ok(new { Message = "Brand deleted" });
    }

    [HttpPatch("{id:int}/toggle-active")]
    public async Task<ActionResult> ToggleActive(int id)
    {
        var brand = await _db.Set<Brand>().FindAsync(id);
        if (brand == null) throw new NotFoundException("Brand not found");

        brand.IsActive = !brand.IsActive;
        await _db.SaveChangesAsync();

        return Ok(new { Message = brand.IsActive ? "Brand activated" : "Brand deactivated", IsActive = brand.IsActive });
    }

    [HttpPatch("{id:int}/toggle-featured")]
    public async Task<ActionResult> ToggleFeatured(int id)
    {
        var brand = await _db.Set<Brand>().FindAsync(id);
        if (brand == null) throw new NotFoundException("Brand not found");

        brand.IsFeatured = !brand.IsFeatured;
        await _db.SaveChangesAsync();

        return Ok(new { Message = brand.IsFeatured ? "Brand featured" : "Brand unfeatured", IsFeatured = brand.IsFeatured });
    }

    [HttpPost("reorder")]
    public async Task<ActionResult> ReorderBrands([FromBody] List<BrandOrderDto> orders)
    {
        foreach (var item in orders)
        {
            var brand = await _db.Set<Brand>().FindAsync(item.Id);
            if (brand != null)
                brand.DisplayOrder = item.DisplayOrder;
        }
        await _db.SaveChangesAsync();
        return Ok(new { Message = "Brands reordered" });
    }

    private static string GenerateSlug(string name)
    {
        var slug = name.ToLowerInvariant();
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"\s+", "-");
        slug = Regex.Replace(slug, @"-+", "-");
        return slug.Trim('-');
    }
}

public class CreateBrandDto
{
    [Required] public required string Name { get; set; }
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public string? BannerUrl { get; set; }
    public string? Website { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsFeatured { get; set; } = false;
    public int DisplayOrder { get; set; } = 0;
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
}

public class UpdateBrandDto
{
    public string? Name { get; set; }
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public string? BannerUrl { get; set; }
    public string? Website { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsFeatured { get; set; }
    public int? DisplayOrder { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
}

public class BrandOrderDto { public int Id { get; set; } public int DisplayOrder { get; set; } }
