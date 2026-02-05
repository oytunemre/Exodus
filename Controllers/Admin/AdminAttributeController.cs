using FarmazonDemo.Data;
using FarmazonDemo.Models.Entities;
using FarmazonDemo.Services.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace FarmazonDemo.Controllers.Admin;

[Route("api/admin/attributes")]
[ApiController]
[Authorize(Policy = "AdminOnly")]
public class AdminAttributeController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public AdminAttributeController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult> GetAttributes([FromQuery] bool? isActive = null, [FromQuery] bool? isFilterable = null)
    {
        var query = _db.Set<ProductAttribute>().Include(a => a.Values).AsQueryable();
        if (isActive.HasValue) query = query.Where(a => a.IsActive == isActive.Value);
        if (isFilterable.HasValue) query = query.Where(a => a.IsFilterable == isFilterable.Value);

        var attributes = await query.OrderBy(a => a.DisplayOrder).ToListAsync();
        return Ok(attributes);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetAttribute(int id)
    {
        var attribute = await _db.Set<ProductAttribute>().Include(a => a.Values).FirstOrDefaultAsync(a => a.Id == id);
        if (attribute == null) throw new NotFoundException("Attribute not found");
        return Ok(attribute);
    }

    [HttpPost]
    public async Task<ActionResult> CreateAttribute([FromBody] CreateAttributeDto dto)
    {
        var attribute = new ProductAttribute {
            Name = dto.Name, Code = dto.Code, Type = dto.Type, IsRequired = dto.IsRequired,
            IsFilterable = dto.IsFilterable, IsVisibleOnProduct = dto.IsVisibleOnProduct, DisplayOrder = dto.DisplayOrder
        };
        _db.Set<ProductAttribute>().Add(attribute);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAttribute), new { id = attribute.Id }, attribute);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> UpdateAttribute(int id, [FromBody] UpdateAttributeDto dto)
    {
        var attribute = await _db.Set<ProductAttribute>().FindAsync(id);
        if (attribute == null) throw new NotFoundException("Attribute not found");

        if (!string.IsNullOrEmpty(dto.Name)) attribute.Name = dto.Name;
        if (dto.Code != null) attribute.Code = dto.Code;
        if (dto.Type.HasValue) attribute.Type = dto.Type.Value;
        if (dto.IsFilterable.HasValue) attribute.IsFilterable = dto.IsFilterable.Value;
        if (dto.IsActive.HasValue) attribute.IsActive = dto.IsActive.Value;
        if (dto.DisplayOrder.HasValue) attribute.DisplayOrder = dto.DisplayOrder.Value;

        await _db.SaveChangesAsync();
        return Ok(attribute);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeleteAttribute(int id)
    {
        var attribute = await _db.Set<ProductAttribute>().FindAsync(id);
        if (attribute == null) throw new NotFoundException("Attribute not found");
        _db.Remove(attribute);
        await _db.SaveChangesAsync();
        return Ok(new { Message = "Attribute deleted" });
    }

    // Attribute Values
    [HttpGet("{attributeId:int}/values")]
    public async Task<ActionResult> GetValues(int attributeId)
    {
        var values = await _db.Set<ProductAttributeValue>().Where(v => v.AttributeId == attributeId).OrderBy(v => v.DisplayOrder).ToListAsync();
        return Ok(values);
    }

    [HttpPost("{attributeId:int}/values")]
    public async Task<ActionResult> CreateValue(int attributeId, [FromBody] CreateAttributeValueDto dto)
    {
        var attribute = await _db.Set<ProductAttribute>().FindAsync(attributeId);
        if (attribute == null) throw new NotFoundException("Attribute not found");

        var value = new ProductAttributeValue { AttributeId = attributeId, Value = dto.Value, Code = dto.Code, ColorHex = dto.ColorHex, ImageUrl = dto.ImageUrl, DisplayOrder = dto.DisplayOrder };
        _db.Set<ProductAttributeValue>().Add(value);
        await _db.SaveChangesAsync();
        return Ok(value);
    }

    [HttpPut("values/{id:int}")]
    public async Task<ActionResult> UpdateValue(int id, [FromBody] UpdateAttributeValueDto dto)
    {
        var value = await _db.Set<ProductAttributeValue>().FindAsync(id);
        if (value == null) throw new NotFoundException("Value not found");

        if (!string.IsNullOrEmpty(dto.Value)) value.Value = dto.Value;
        if (dto.Code != null) value.Code = dto.Code;
        if (dto.ColorHex != null) value.ColorHex = dto.ColorHex;
        if (dto.IsActive.HasValue) value.IsActive = dto.IsActive.Value;
        if (dto.DisplayOrder.HasValue) value.DisplayOrder = dto.DisplayOrder.Value;

        await _db.SaveChangesAsync();
        return Ok(value);
    }

    [HttpDelete("values/{id:int}")]
    public async Task<ActionResult> DeleteValue(int id)
    {
        var value = await _db.Set<ProductAttributeValue>().FindAsync(id);
        if (value == null) throw new NotFoundException("Value not found");
        _db.Remove(value);
        await _db.SaveChangesAsync();
        return Ok(new { Message = "Value deleted" });
    }
}

public class CreateAttributeDto { [Required] public required string Name { get; set; } public string? Code { get; set; } public AttributeType Type { get; set; } public bool IsRequired { get; set; } public bool IsFilterable { get; set; } = true; public bool IsVisibleOnProduct { get; set; } = true; public int DisplayOrder { get; set; } }
public class UpdateAttributeDto { public string? Name { get; set; } public string? Code { get; set; } public AttributeType? Type { get; set; } public bool? IsFilterable { get; set; } public bool? IsActive { get; set; } public int? DisplayOrder { get; set; } }
public class CreateAttributeValueDto { [Required] public required string Value { get; set; } public string? Code { get; set; } public string? ColorHex { get; set; } public string? ImageUrl { get; set; } public int DisplayOrder { get; set; } }
public class UpdateAttributeValueDto { public string? Value { get; set; } public string? Code { get; set; } public string? ColorHex { get; set; } public bool? IsActive { get; set; } public int? DisplayOrder { get; set; } }
