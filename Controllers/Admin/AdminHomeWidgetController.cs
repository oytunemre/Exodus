using Exodus.Data;
using Exodus.Models.Entities;
using Exodus.Services.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Exodus.Controllers.Admin;

[Route("api/admin/home-widgets")]
[ApiController]
[Authorize(Policy = "AdminOnly")]
public class AdminHomeWidgetController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public AdminHomeWidgetController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult> GetWidgets(
        [FromQuery] HomeWidgetType? type = null,
        [FromQuery] HomeWidgetPosition? position = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] bool includeScheduled = true)
    {
        var query = _db.Set<HomeWidget>().AsQueryable();

        if (type.HasValue)
            query = query.Where(w => w.Type == type.Value);

        if (position.HasValue)
            query = query.Where(w => w.Position == position.Value);

        if (isActive.HasValue)
            query = query.Where(w => w.IsActive == isActive.Value);

        if (!includeScheduled)
        {
            var now = DateTime.UtcNow;
            query = query.Where(w =>
                (!w.StartDate.HasValue || w.StartDate <= now) &&
                (!w.EndDate.HasValue || w.EndDate >= now));
        }

        var widgets = await query
            .OrderBy(w => w.Position)
            .ThenBy(w => w.DisplayOrder)
            .Select(w => new
            {
                w.Id,
                w.Name,
                w.Code,
                w.Type,
                TypeName = w.Type.ToString(),
                w.Title,
                w.Subtitle,
                w.Position,
                PositionName = w.Position.ToString(),
                w.DisplayOrder,
                w.IsActive,
                w.ShowOnMobile,
                w.ShowOnDesktop,
                w.StartDate,
                w.EndDate,
                IsScheduled = w.StartDate.HasValue || w.EndDate.HasValue,
                IsCurrentlyVisible = w.IsActive &&
                    (!w.StartDate.HasValue || w.StartDate <= DateTime.UtcNow) &&
                    (!w.EndDate.HasValue || w.EndDate >= DateTime.UtcNow),
                w.CreatedAt,
                w.UpdatedAt
            })
            .ToListAsync();

        return Ok(widgets);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetWidget(int id)
    {
        var widget = await _db.Set<HomeWidget>().FindAsync(id);
        if (widget == null) throw new NotFoundException("Widget not found");
        return Ok(widget);
    }

    [HttpGet("preview")]
    public async Task<ActionResult> PreviewHomepage()
    {
        var now = DateTime.UtcNow;

        var widgets = await _db.Set<HomeWidget>()
            .Where(w => w.IsActive &&
                (!w.StartDate.HasValue || w.StartDate <= now) &&
                (!w.EndDate.HasValue || w.EndDate >= now))
            .OrderBy(w => w.Position)
            .ThenBy(w => w.DisplayOrder)
            .ToListAsync();

        var grouped = widgets
            .GroupBy(w => w.Position)
            .Select(g => new
            {
                Position = g.Key.ToString(),
                Widgets = g.Select(w => new
                {
                    w.Id,
                    w.Name,
                    w.Type,
                    TypeName = w.Type.ToString(),
                    w.Title,
                    w.Subtitle,
                    w.Configuration,
                    w.ShowOnMobile,
                    w.ShowOnDesktop
                })
            })
            .ToList();

        return Ok(grouped);
    }

    [HttpPost]
    public async Task<ActionResult> CreateWidget([FromBody] CreateHomeWidgetDto dto)
    {
        var widget = new HomeWidget
        {
            Name = dto.Name,
            Code = dto.Code,
            Type = dto.Type,
            Title = dto.Title,
            Subtitle = dto.Subtitle,
            Configuration = dto.Configuration,
            CategoryId = dto.CategoryId,
            BrandId = dto.BrandId,
            CampaignId = dto.CampaignId,
            ProductIds = dto.ProductIds,
            ItemCount = dto.ItemCount,
            Position = dto.Position,
            DisplayOrder = dto.DisplayOrder,
            IsActive = dto.IsActive,
            ShowOnMobile = dto.ShowOnMobile,
            ShowOnDesktop = dto.ShowOnDesktop,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate
        };

        _db.Set<HomeWidget>().Add(widget);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetWidget), new { id = widget.Id }, widget);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> UpdateWidget(int id, [FromBody] UpdateHomeWidgetDto dto)
    {
        var widget = await _db.Set<HomeWidget>().FindAsync(id);
        if (widget == null) throw new NotFoundException("Widget not found");

        if (!string.IsNullOrEmpty(dto.Name)) widget.Name = dto.Name;
        if (dto.Code != null) widget.Code = dto.Code;
        if (dto.Type.HasValue) widget.Type = dto.Type.Value;
        if (dto.Title != null) widget.Title = dto.Title;
        if (dto.Subtitle != null) widget.Subtitle = dto.Subtitle;
        if (dto.Configuration != null) widget.Configuration = dto.Configuration;
        if (dto.CategoryId.HasValue) widget.CategoryId = dto.CategoryId;
        if (dto.BrandId.HasValue) widget.BrandId = dto.BrandId;
        if (dto.CampaignId.HasValue) widget.CampaignId = dto.CampaignId;
        if (dto.ProductIds != null) widget.ProductIds = dto.ProductIds;
        if (dto.ItemCount.HasValue) widget.ItemCount = dto.ItemCount.Value;
        if (dto.Position.HasValue) widget.Position = dto.Position.Value;
        if (dto.DisplayOrder.HasValue) widget.DisplayOrder = dto.DisplayOrder.Value;
        if (dto.IsActive.HasValue) widget.IsActive = dto.IsActive.Value;
        if (dto.ShowOnMobile.HasValue) widget.ShowOnMobile = dto.ShowOnMobile.Value;
        if (dto.ShowOnDesktop.HasValue) widget.ShowOnDesktop = dto.ShowOnDesktop.Value;
        if (dto.StartDate.HasValue) widget.StartDate = dto.StartDate;
        if (dto.EndDate.HasValue) widget.EndDate = dto.EndDate;

        await _db.SaveChangesAsync();
        return Ok(widget);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeleteWidget(int id)
    {
        var widget = await _db.Set<HomeWidget>().FindAsync(id);
        if (widget == null) throw new NotFoundException("Widget not found");

        _db.Remove(widget);
        await _db.SaveChangesAsync();

        return Ok(new { Message = "Widget deleted" });
    }

    [HttpPatch("{id:int}/toggle-active")]
    public async Task<ActionResult> ToggleActive(int id)
    {
        var widget = await _db.Set<HomeWidget>().FindAsync(id);
        if (widget == null) throw new NotFoundException("Widget not found");

        widget.IsActive = !widget.IsActive;
        await _db.SaveChangesAsync();

        return Ok(new { Message = widget.IsActive ? "Widget activated" : "Widget deactivated", IsActive = widget.IsActive });
    }

    [HttpPost("reorder")]
    public async Task<ActionResult> ReorderWidgets([FromBody] List<WidgetOrderDto> orders)
    {
        foreach (var item in orders)
        {
            var widget = await _db.Set<HomeWidget>().FindAsync(item.Id);
            if (widget != null)
            {
                widget.DisplayOrder = item.DisplayOrder;
                if (item.Position.HasValue)
                    widget.Position = item.Position.Value;
            }
        }
        await _db.SaveChangesAsync();
        return Ok(new { Message = "Widgets reordered" });
    }

    [HttpPost("{id:int}/duplicate")]
    public async Task<ActionResult> DuplicateWidget(int id)
    {
        var original = await _db.Set<HomeWidget>().FindAsync(id);
        if (original == null) throw new NotFoundException("Widget not found");

        var copy = new HomeWidget
        {
            Name = original.Name + " (Copy)",
            Code = original.Code != null ? original.Code + "_copy" : null,
            Type = original.Type,
            Title = original.Title,
            Subtitle = original.Subtitle,
            Configuration = original.Configuration,
            CategoryId = original.CategoryId,
            BrandId = original.BrandId,
            CampaignId = original.CampaignId,
            ProductIds = original.ProductIds,
            ItemCount = original.ItemCount,
            Position = original.Position,
            DisplayOrder = original.DisplayOrder + 1,
            IsActive = false,
            ShowOnMobile = original.ShowOnMobile,
            ShowOnDesktop = original.ShowOnDesktop
        };

        _db.Set<HomeWidget>().Add(copy);
        await _db.SaveChangesAsync();

        return Ok(copy);
    }

    [HttpGet("types")]
    public ActionResult GetWidgetTypes()
    {
        var types = Enum.GetValues<HomeWidgetType>()
            .Select(t => new { Value = (int)t, Name = t.ToString() })
            .ToList();
        return Ok(types);
    }

    [HttpGet("positions")]
    public ActionResult GetWidgetPositions()
    {
        var positions = Enum.GetValues<HomeWidgetPosition>()
            .Select(p => new { Value = (int)p, Name = p.ToString() })
            .ToList();
        return Ok(positions);
    }
}

public class CreateHomeWidgetDto
{
    [Required] public required string Name { get; set; }
    public string? Code { get; set; }
    public HomeWidgetType Type { get; set; }
    public string? Title { get; set; }
    public string? Subtitle { get; set; }
    public string? Configuration { get; set; }
    public int? CategoryId { get; set; }
    public int? BrandId { get; set; }
    public int? CampaignId { get; set; }
    public string? ProductIds { get; set; }
    public int ItemCount { get; set; } = 10;
    public HomeWidgetPosition Position { get; set; } = HomeWidgetPosition.Main;
    public int DisplayOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public bool ShowOnMobile { get; set; } = true;
    public bool ShowOnDesktop { get; set; } = true;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class UpdateHomeWidgetDto
{
    public string? Name { get; set; }
    public string? Code { get; set; }
    public HomeWidgetType? Type { get; set; }
    public string? Title { get; set; }
    public string? Subtitle { get; set; }
    public string? Configuration { get; set; }
    public int? CategoryId { get; set; }
    public int? BrandId { get; set; }
    public int? CampaignId { get; set; }
    public string? ProductIds { get; set; }
    public int? ItemCount { get; set; }
    public HomeWidgetPosition? Position { get; set; }
    public int? DisplayOrder { get; set; }
    public bool? IsActive { get; set; }
    public bool? ShowOnMobile { get; set; }
    public bool? ShowOnDesktop { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class WidgetOrderDto
{
    public int Id { get; set; }
    public int DisplayOrder { get; set; }
    public HomeWidgetPosition? Position { get; set; }
}
