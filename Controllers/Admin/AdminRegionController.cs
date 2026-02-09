using Exodus.Data;
using Exodus.Models.Entities;
using Exodus.Services.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Exodus.Controllers.Admin;

[Route("api/admin/regions")]
[ApiController]
[Authorize(Policy = "AdminOnly")]
public class AdminRegionController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public AdminRegionController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult> GetRegions([FromQuery] RegionType? type = null, [FromQuery] int? parentId = null)
    {
        var query = _db.Set<Region>().AsQueryable();
        if (type.HasValue) query = query.Where(r => r.Type == type.Value);
        if (parentId.HasValue) query = query.Where(r => r.ParentId == parentId.Value);
        else if (!type.HasValue) query = query.Where(r => r.ParentId == null);

        var regions = await query.OrderBy(r => r.DisplayOrder).ThenBy(r => r.Name).ToListAsync();
        return Ok(regions);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetRegion(int id)
    {
        var region = await _db.Set<Region>().Include(r => r.Children).FirstOrDefaultAsync(r => r.Id == id);
        if (region == null) throw new NotFoundException("Region not found");
        return Ok(region);
    }

    [HttpGet("tree")]
    public async Task<ActionResult> GetRegionTree()
    {
        var provinces = await _db.Set<Region>().Where(r => r.Type == RegionType.Province).OrderBy(r => r.Name).ToListAsync();
        var districts = await _db.Set<Region>().Where(r => r.Type == RegionType.District).ToListAsync();

        var tree = provinces.Select(p => new {
            p.Id, p.Name, p.Code, p.IsActive,
            Districts = districts.Where(d => d.ParentId == p.Id).OrderBy(d => d.Name).Select(d => new { d.Id, d.Name, d.Code, d.IsActive })
        }).ToList();

        return Ok(tree);
    }

    [HttpPost]
    public async Task<ActionResult> CreateRegion([FromBody] CreateRegionDto dto)
    {
        var region = new Region { Name = dto.Name, Code = dto.Code, Type = dto.Type, ParentId = dto.ParentId, IsActive = dto.IsActive, DisplayOrder = dto.DisplayOrder };
        _db.Set<Region>().Add(region);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetRegion), new { id = region.Id }, region);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> UpdateRegion(int id, [FromBody] UpdateRegionDto dto)
    {
        var region = await _db.Set<Region>().FindAsync(id);
        if (region == null) throw new NotFoundException("Region not found");

        if (!string.IsNullOrEmpty(dto.Name)) region.Name = dto.Name;
        if (dto.Code != null) region.Code = dto.Code;
        if (dto.IsActive.HasValue) region.IsActive = dto.IsActive.Value;
        if (dto.DisplayOrder.HasValue) region.DisplayOrder = dto.DisplayOrder.Value;

        await _db.SaveChangesAsync();
        return Ok(region);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeleteRegion(int id)
    {
        var region = await _db.Set<Region>().Include(r => r.Children).FirstOrDefaultAsync(r => r.Id == id);
        if (region == null) throw new NotFoundException("Region not found");
        if (region.Children.Any()) throw new BadRequestException("Cannot delete region with children");

        _db.Remove(region);
        await _db.SaveChangesAsync();
        return Ok(new { Message = "Region deleted" });
    }

    // Shipping Zones
    [HttpGet("zones")]
    public async Task<ActionResult> GetShippingZones() => Ok(await _db.Set<ShippingZone>().OrderBy(z => z.DisplayOrder).ToListAsync());

    [HttpPost("zones")]
    public async Task<ActionResult> CreateShippingZone([FromBody] CreateShippingZoneDto dto)
    {
        var zone = new ShippingZone { Name = dto.Name, Description = dto.Description, BaseShippingCost = dto.BaseShippingCost,
            FreeShippingThreshold = dto.FreeShippingThreshold, EstimatedDeliveryDays = dto.EstimatedDeliveryDays, IsActive = dto.IsActive };
        _db.Set<ShippingZone>().Add(zone);
        await _db.SaveChangesAsync();
        return Ok(zone);
    }

    [HttpPut("zones/{id:int}")]
    public async Task<ActionResult> UpdateShippingZone(int id, [FromBody] UpdateShippingZoneDto dto)
    {
        var zone = await _db.Set<ShippingZone>().FindAsync(id);
        if (zone == null) throw new NotFoundException("Shipping zone not found");

        if (!string.IsNullOrEmpty(dto.Name)) zone.Name = dto.Name;
        if (dto.BaseShippingCost.HasValue) zone.BaseShippingCost = dto.BaseShippingCost.Value;
        if (dto.FreeShippingThreshold.HasValue) zone.FreeShippingThreshold = dto.FreeShippingThreshold;
        if (dto.EstimatedDeliveryDays.HasValue) zone.EstimatedDeliveryDays = dto.EstimatedDeliveryDays.Value;
        if (dto.IsActive.HasValue) zone.IsActive = dto.IsActive.Value;

        await _db.SaveChangesAsync();
        return Ok(zone);
    }
}

public class CreateRegionDto { [Required] public required string Name { get; set; } public string? Code { get; set; } public RegionType Type { get; set; } public int? ParentId { get; set; } public bool IsActive { get; set; } = true; public int DisplayOrder { get; set; } }
public class UpdateRegionDto { public string? Name { get; set; } public string? Code { get; set; } public bool? IsActive { get; set; } public int? DisplayOrder { get; set; } }
public class CreateShippingZoneDto { [Required] public required string Name { get; set; } public string? Description { get; set; } public decimal BaseShippingCost { get; set; } public decimal? FreeShippingThreshold { get; set; } public int EstimatedDeliveryDays { get; set; } = 3; public bool IsActive { get; set; } = true; }
public class UpdateShippingZoneDto { public string? Name { get; set; } public decimal? BaseShippingCost { get; set; } public decimal? FreeShippingThreshold { get; set; } public int? EstimatedDeliveryDays { get; set; } public bool? IsActive { get; set; } }
