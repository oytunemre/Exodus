using Exodus.Data;
using Exodus.Models.Entities;
using Exodus.Services.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Exodus.Controllers.Admin;

[Route("api/admin/taxes")]
[ApiController]
[Authorize(Policy = "AdminOnly")]
public class AdminTaxController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public AdminTaxController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult> GetTaxRates([FromQuery] bool? isActive = null)
    {
        var query = _db.Set<TaxRate>().AsQueryable();
        if (isActive.HasValue) query = query.Where(t => t.IsActive == isActive.Value);

        var rates = await query.OrderBy(t => t.DisplayOrder).ToListAsync();
        return Ok(rates);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetTaxRate(int id)
    {
        var rate = await _db.Set<TaxRate>().FindAsync(id);
        if (rate == null) throw new NotFoundException("Tax rate not found");
        return Ok(rate);
    }

    [HttpPost]
    public async Task<ActionResult> CreateTaxRate([FromBody] CreateTaxRateDto dto)
    {
        var rate = new TaxRate {
            Name = dto.Name, Code = dto.Code, Rate = dto.Rate, IsDefault = dto.IsDefault,
            IsActive = dto.IsActive, AppliesToAllCategories = dto.AppliesToAllCategories,
            ApplicableCategoryIds = dto.ApplicableCategoryIds, DisplayOrder = dto.DisplayOrder
        };

        if (dto.IsDefault) {
            var existing = await _db.Set<TaxRate>().Where(t => t.IsDefault).ToListAsync();
            existing.ForEach(t => t.IsDefault = false);
        }

        _db.Set<TaxRate>().Add(rate);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetTaxRate), new { id = rate.Id }, rate);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> UpdateTaxRate(int id, [FromBody] UpdateTaxRateDto dto)
    {
        var rate = await _db.Set<TaxRate>().FindAsync(id);
        if (rate == null) throw new NotFoundException("Tax rate not found");

        if (!string.IsNullOrEmpty(dto.Name)) rate.Name = dto.Name;
        if (dto.Code != null) rate.Code = dto.Code;
        if (dto.Rate.HasValue) rate.Rate = dto.Rate.Value;
        if (dto.IsDefault.HasValue) {
            if (dto.IsDefault.Value) {
                var existing = await _db.Set<TaxRate>().Where(t => t.IsDefault && t.Id != id).ToListAsync();
                existing.ForEach(t => t.IsDefault = false);
            }
            rate.IsDefault = dto.IsDefault.Value;
        }
        if (dto.IsActive.HasValue) rate.IsActive = dto.IsActive.Value;
        if (dto.DisplayOrder.HasValue) rate.DisplayOrder = dto.DisplayOrder.Value;

        await _db.SaveChangesAsync();
        return Ok(rate);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeleteTaxRate(int id)
    {
        var rate = await _db.Set<TaxRate>().FindAsync(id);
        if (rate == null) throw new NotFoundException("Tax rate not found");
        if (rate.IsDefault) throw new BadRequestException("Cannot delete default tax rate");

        _db.Remove(rate);
        await _db.SaveChangesAsync();
        return Ok(new { Message = "Tax rate deleted" });
    }
}

public class CreateTaxRateDto {
    [Required] public required string Name { get; set; }
    public string? Code { get; set; }
    [Range(0, 100)] public decimal Rate { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public bool AppliesToAllCategories { get; set; } = true;
    public string? ApplicableCategoryIds { get; set; }
    public int DisplayOrder { get; set; }
}

public class UpdateTaxRateDto {
    public string? Name { get; set; }
    public string? Code { get; set; }
    public decimal? Rate { get; set; }
    public bool? IsDefault { get; set; }
    public bool? IsActive { get; set; }
    public int? DisplayOrder { get; set; }
}
