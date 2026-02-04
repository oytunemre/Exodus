using FarmazonDemo.Data;
using FarmazonDemo.Models.Entities;
using FarmazonDemo.Services.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace FarmazonDemo.Controllers.Admin;

[Route("api/admin/settings")]
[ApiController]
[Authorize(Policy = "AdminOnly")]
public class AdminSettingsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public AdminSettingsController(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Get all settings
    /// </summary>
    [HttpGet]
    public async Task<ActionResult> GetAllSettings([FromQuery] SettingCategory? category = null)
    {
        var query = _db.SiteSettings.AsQueryable();

        if (category.HasValue)
            query = query.Where(s => s.Category == category.Value);

        var settings = await query
            .OrderBy(s => s.Category)
            .ThenBy(s => s.Key)
            .Select(s => new
            {
                s.Id,
                s.Key,
                s.Value,
                s.Description,
                s.Category,
                s.IsPublic,
                s.UpdatedAt
            })
            .ToListAsync();

        return Ok(settings);
    }

    /// <summary>
    /// Get setting by key
    /// </summary>
    [HttpGet("{key}")]
    public async Task<ActionResult> GetSetting(string key)
    {
        var setting = await _db.SiteSettings
            .FirstOrDefaultAsync(s => s.Key == key);

        if (setting == null)
            throw new NotFoundException($"Setting '{key}' not found");

        return Ok(setting);
    }

    /// <summary>
    /// Update setting value
    /// </summary>
    [HttpPut("{key}")]
    public async Task<ActionResult> UpdateSetting(string key, [FromBody] UpdateSettingDto dto)
    {
        var setting = await _db.SiteSettings
            .FirstOrDefaultAsync(s => s.Key == key);

        if (setting == null)
            throw new NotFoundException($"Setting '{key}' not found");

        setting.Value = dto.Value;

        if (dto.Description != null)
            setting.Description = dto.Description;

        if (dto.IsPublic.HasValue)
            setting.IsPublic = dto.IsPublic.Value;

        await _db.SaveChangesAsync();

        return Ok(new { Message = "Setting updated", Key = key, Value = dto.Value });
    }

    /// <summary>
    /// Create new setting
    /// </summary>
    [HttpPost]
    public async Task<ActionResult> CreateSetting([FromBody] CreateSettingDto dto)
    {
        var exists = await _db.SiteSettings.AnyAsync(s => s.Key == dto.Key);
        if (exists)
            throw new BadRequestException($"Setting '{dto.Key}' already exists");

        var setting = new SiteSetting
        {
            Key = dto.Key,
            Value = dto.Value,
            Description = dto.Description,
            Category = dto.Category,
            IsPublic = dto.IsPublic
        };

        _db.SiteSettings.Add(setting);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetSetting), new { key = setting.Key }, new { Message = "Setting created", Key = setting.Key });
    }

    /// <summary>
    /// Delete setting
    /// </summary>
    [HttpDelete("{key}")]
    public async Task<ActionResult> DeleteSetting(string key)
    {
        var setting = await _db.SiteSettings
            .FirstOrDefaultAsync(s => s.Key == key);

        if (setting == null)
            throw new NotFoundException($"Setting '{key}' not found");

        _db.SiteSettings.Remove(setting);
        await _db.SaveChangesAsync();

        return Ok(new { Message = "Setting deleted", Key = key });
    }

    /// <summary>
    /// Bulk update settings
    /// </summary>
    [HttpPost("bulk-update")]
    public async Task<ActionResult> BulkUpdate([FromBody] BulkUpdateSettingsDto dto)
    {
        var keys = dto.Settings.Select(s => s.Key).ToList();
        var settings = await _db.SiteSettings
            .Where(s => keys.Contains(s.Key))
            .ToListAsync();

        foreach (var update in dto.Settings)
        {
            var setting = settings.FirstOrDefault(s => s.Key == update.Key);
            if (setting != null)
            {
                setting.Value = update.Value;
            }
        }

        await _db.SaveChangesAsync();

        return Ok(new { Message = $"{settings.Count} settings updated" });
    }

    // ============ Convenience endpoints for specific setting groups ============

    /// <summary>
    /// Get shipping settings
    /// </summary>
    [HttpGet("shipping")]
    public async Task<ActionResult> GetShippingSettings()
    {
        var settings = await _db.SiteSettings
            .Where(s => s.Category == SettingCategory.Shipping)
            .ToListAsync();

        return Ok(new
        {
            DefaultShippingCost = GetDecimalValue(settings, SettingKeys.DefaultShippingCost, 29.90m),
            FreeShippingThreshold = GetDecimalValue(settings, SettingKeys.FreeShippingThreshold, 500m),
            ShippingTaxRate = GetDecimalValue(settings, SettingKeys.ShippingTaxRate, 20m)
        });
    }

    /// <summary>
    /// Update shipping settings
    /// </summary>
    [HttpPut("shipping")]
    public async Task<ActionResult> UpdateShippingSettings([FromBody] ShippingSettingsDto dto)
    {
        await UpsertSettingAsync(SettingKeys.DefaultShippingCost, dto.DefaultShippingCost.ToString(), SettingCategory.Shipping, "Varsayılan kargo ücreti (TL)");
        await UpsertSettingAsync(SettingKeys.FreeShippingThreshold, dto.FreeShippingThreshold.ToString(), SettingCategory.Shipping, "Ücretsiz kargo limiti (TL)");

        if (dto.ShippingTaxRate.HasValue)
            await UpsertSettingAsync(SettingKeys.ShippingTaxRate, dto.ShippingTaxRate.Value.ToString(), SettingCategory.Shipping, "Kargo KDV oranı (%)");

        await _db.SaveChangesAsync();

        return Ok(new { Message = "Shipping settings updated" });
    }

    /// <summary>
    /// Get commission settings
    /// </summary>
    [HttpGet("commission")]
    public async Task<ActionResult> GetCommissionSettings()
    {
        var settings = await _db.SiteSettings
            .Where(s => s.Category == SettingCategory.Commission)
            .ToListAsync();

        return Ok(new
        {
            DefaultCommissionRate = GetDecimalValue(settings, SettingKeys.DefaultCommissionRate, 10m),
            MinCommissionAmount = GetDecimalValue(settings, SettingKeys.MinCommissionAmount, 1m)
        });
    }

    /// <summary>
    /// Update commission settings
    /// </summary>
    [HttpPut("commission")]
    public async Task<ActionResult> UpdateCommissionSettings([FromBody] CommissionSettingsDto dto)
    {
        if (dto.DefaultCommissionRate < 0 || dto.DefaultCommissionRate > 100)
            throw new BadRequestException("Commission rate must be between 0 and 100");

        await UpsertSettingAsync(SettingKeys.DefaultCommissionRate, dto.DefaultCommissionRate.ToString(), SettingCategory.Commission, "Varsayılan komisyon oranı (%)");

        if (dto.MinCommissionAmount.HasValue)
            await UpsertSettingAsync(SettingKeys.MinCommissionAmount, dto.MinCommissionAmount.Value.ToString(), SettingCategory.Commission, "Minimum komisyon tutarı (TL)");

        await _db.SaveChangesAsync();

        return Ok(new { Message = "Commission settings updated" });
    }

    /// <summary>
    /// Get general settings
    /// </summary>
    [HttpGet("general")]
    public async Task<ActionResult> GetGeneralSettings()
    {
        var settings = await _db.SiteSettings
            .Where(s => s.Category == SettingCategory.General)
            .ToListAsync();

        return Ok(new
        {
            SiteName = GetStringValue(settings, SettingKeys.SiteName, "Farmazon"),
            SiteDescription = GetStringValue(settings, SettingKeys.SiteDescription, ""),
            ContactEmail = GetStringValue(settings, SettingKeys.ContactEmail, ""),
            ContactPhone = GetStringValue(settings, SettingKeys.ContactPhone, "")
        });
    }

    /// <summary>
    /// Update general settings
    /// </summary>
    [HttpPut("general")]
    public async Task<ActionResult> UpdateGeneralSettings([FromBody] GeneralSettingsDto dto)
    {
        if (!string.IsNullOrEmpty(dto.SiteName))
            await UpsertSettingAsync(SettingKeys.SiteName, dto.SiteName, SettingCategory.General, "Site adı", true);

        if (dto.SiteDescription != null)
            await UpsertSettingAsync(SettingKeys.SiteDescription, dto.SiteDescription, SettingCategory.General, "Site açıklaması", true);

        if (dto.ContactEmail != null)
            await UpsertSettingAsync(SettingKeys.ContactEmail, dto.ContactEmail, SettingCategory.General, "İletişim e-postası", true);

        if (dto.ContactPhone != null)
            await UpsertSettingAsync(SettingKeys.ContactPhone, dto.ContactPhone, SettingCategory.General, "İletişim telefonu", true);

        await _db.SaveChangesAsync();

        return Ok(new { Message = "General settings updated" });
    }

    // ============ Helper methods ============

    private async Task UpsertSettingAsync(string key, string value, SettingCategory category, string? description = null, bool isPublic = false)
    {
        var setting = await _db.SiteSettings.FirstOrDefaultAsync(s => s.Key == key);

        if (setting == null)
        {
            setting = new SiteSetting
            {
                Key = key,
                Value = value,
                Description = description,
                Category = category,
                IsPublic = isPublic
            };
            _db.SiteSettings.Add(setting);
        }
        else
        {
            setting.Value = value;
            if (description != null)
                setting.Description = description;
        }
    }

    private static decimal GetDecimalValue(List<SiteSetting> settings, string key, decimal defaultValue)
    {
        var setting = settings.FirstOrDefault(s => s.Key == key);
        if (setting != null && decimal.TryParse(setting.Value, out var value))
            return value;
        return defaultValue;
    }

    private static string GetStringValue(List<SiteSetting> settings, string key, string defaultValue)
    {
        var setting = settings.FirstOrDefault(s => s.Key == key);
        return setting?.Value ?? defaultValue;
    }
}

// Public endpoint for public settings
[Route("api/settings")]
[ApiController]
public class SettingsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public SettingsController(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Get public settings
    /// </summary>
    [HttpGet]
    public async Task<ActionResult> GetPublicSettings()
    {
        var settings = await _db.SiteSettings
            .Where(s => s.IsPublic)
            .Select(s => new { s.Key, s.Value })
            .ToListAsync();

        var result = settings.ToDictionary(s => s.Key, s => s.Value);
        return Ok(result);
    }

    /// <summary>
    /// Get shipping info (for cart/checkout)
    /// </summary>
    [HttpGet("shipping")]
    public async Task<ActionResult> GetShippingInfo()
    {
        var settings = await _db.SiteSettings
            .Where(s => s.Category == SettingCategory.Shipping)
            .ToListAsync();

        var defaultCost = settings.FirstOrDefault(s => s.Key == SettingKeys.DefaultShippingCost);
        var freeThreshold = settings.FirstOrDefault(s => s.Key == SettingKeys.FreeShippingThreshold);

        return Ok(new
        {
            ShippingCost = decimal.TryParse(defaultCost?.Value, out var cost) ? cost : 29.90m,
            FreeShippingThreshold = decimal.TryParse(freeThreshold?.Value, out var threshold) ? threshold : 500m
        });
    }
}

public class UpdateSettingDto
{
    [Required]
    public required string Value { get; set; }
    public string? Description { get; set; }
    public bool? IsPublic { get; set; }
}

public class CreateSettingDto
{
    [Required]
    [StringLength(100)]
    public required string Key { get; set; }

    [Required]
    [StringLength(2000)]
    public required string Value { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    public SettingCategory Category { get; set; } = SettingCategory.General;

    public bool IsPublic { get; set; } = false;
}

public class BulkUpdateSettingsDto
{
    public required List<SettingUpdateItem> Settings { get; set; }
}

public class SettingUpdateItem
{
    public required string Key { get; set; }
    public required string Value { get; set; }
}

public class ShippingSettingsDto
{
    [Range(0, 10000)]
    public decimal DefaultShippingCost { get; set; }

    [Range(0, 100000)]
    public decimal FreeShippingThreshold { get; set; }

    [Range(0, 100)]
    public decimal? ShippingTaxRate { get; set; }
}

public class CommissionSettingsDto
{
    [Range(0, 100)]
    public decimal DefaultCommissionRate { get; set; }

    [Range(0, 10000)]
    public decimal? MinCommissionAmount { get; set; }
}

public class GeneralSettingsDto
{
    [StringLength(100)]
    public string? SiteName { get; set; }

    [StringLength(500)]
    public string? SiteDescription { get; set; }

    [EmailAddress]
    [StringLength(100)]
    public string? ContactEmail { get; set; }

    [Phone]
    [StringLength(20)]
    public string? ContactPhone { get; set; }
}
