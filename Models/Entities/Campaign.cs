using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmazonDemo.Models.Entities;

public class Campaign : BaseEntity
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required]
    public CampaignType Type { get; set; }

    [Required]
    public int SellerId { get; set; }

    [ForeignKey(nameof(SellerId))]
    public Users? Seller { get; set; }

    // Campaign validity
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; } = true;

    // Usage limits
    public int? MaxUsageCount { get; set; }
    public int? MaxUsagePerUser { get; set; }
    public int CurrentUsageCount { get; set; } = 0;

    // Minimum requirements
    public decimal? MinimumOrderAmount { get; set; }
    public int? MinimumQuantity { get; set; }

    // Discount values
    public decimal? DiscountPercentage { get; set; }
    public decimal? DiscountAmount { get; set; }
    public decimal? MaxDiscountAmount { get; set; }

    // BOGO specific (Buy X Get Y Free)
    public int? BuyQuantity { get; set; }
    public int? GetQuantity { get; set; }

    // Coupon code (optional)
    [StringLength(50)]
    public string? CouponCode { get; set; }
    public bool RequiresCouponCode { get; set; } = false;

    // Target scope
    public CampaignScope Scope { get; set; } = CampaignScope.AllProducts;

    // Priority (higher = applied first)
    public int Priority { get; set; } = 0;

    // Stackable with other campaigns?
    public bool IsStackable { get; set; } = false;

    // Relations
    public ICollection<CampaignProduct> CampaignProducts { get; set; } = new List<CampaignProduct>();
    public ICollection<CampaignCategory> CampaignCategories { get; set; } = new List<CampaignCategory>();
    public ICollection<CampaignUsage> Usages { get; set; } = new List<CampaignUsage>();
}

public enum CampaignType
{
    PercentageDiscount = 0,      // %20 indirim
    FixedAmountDiscount = 1,     // 50 TL indirim
    BuyXGetYFree = 2,            // 1 al 1 bedava, 2 al 1 bedava
    BuyXPayY = 3,                // 3 al 2 ode
    FreeShipping = 4,            // Ucretsiz kargo
    MinimumAmountDiscount = 5    // 500 TL ve uzeri %10 indirim
}

public enum CampaignScope
{
    AllProducts = 0,             // Tum urunler
    SpecificProducts = 1,        // Belirli urunler
    SpecificCategories = 2,      // Belirli kategoriler
    SpecificListings = 3         // Belirli listing'ler
}
