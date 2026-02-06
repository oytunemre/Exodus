using System.ComponentModel.DataAnnotations;

namespace Exodus.Models.Entities;

public class SiteSetting : BaseEntity
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

public enum SettingCategory
{
    General = 0,
    Shipping = 1,
    Commission = 2,
    Payment = 3,
    Email = 4,
    Seo = 5
}

// Pre-defined setting keys
public static class SettingKeys
{
    // Shipping
    public const string DefaultShippingCost = "Shipping.DefaultCost";
    public const string FreeShippingThreshold = "Shipping.FreeThreshold";
    public const string ShippingTaxRate = "Shipping.TaxRate";

    // Commission
    public const string DefaultCommissionRate = "Commission.DefaultRate";
    public const string MinCommissionAmount = "Commission.MinAmount";

    // General
    public const string SiteName = "General.SiteName";
    public const string SiteDescription = "General.Description";
    public const string ContactEmail = "General.ContactEmail";
    public const string ContactPhone = "General.ContactPhone";

    // Payment
    public const string MinOrderAmount = "Payment.MinOrderAmount";
    public const string MaxOrderAmount = "Payment.MaxOrderAmount";
}
