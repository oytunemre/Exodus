using System.ComponentModel.DataAnnotations;
using Exodus.Models.Entities;

namespace Exodus.Models.Dto.Campaign;

public class CampaignDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public CampaignType Type { get; set; }
    public string TypeDisplay => Type.ToString();
    public int SellerId { get; set; }
    public string? SellerName { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
    public bool IsCurrentlyValid { get; set; }
    public int? MaxUsageCount { get; set; }
    public int? MaxUsagePerUser { get; set; }
    public int CurrentUsageCount { get; set; }
    public decimal? MinimumOrderAmount { get; set; }
    public int? MinimumQuantity { get; set; }
    public decimal? DiscountPercentage { get; set; }
    public decimal? DiscountAmount { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public int? BuyQuantity { get; set; }
    public int? GetQuantity { get; set; }
    public string? CouponCode { get; set; }
    public bool RequiresCouponCode { get; set; }
    public CampaignScope Scope { get; set; }
    public string ScopeDisplay => Scope.ToString();
    public int Priority { get; set; }
    public bool IsStackable { get; set; }
    public List<int>? ProductIds { get; set; }
    public List<int>? ListingIds { get; set; }
    public List<int>? CategoryIds { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateCampaignDto
{
    [Required]
    [StringLength(200)]
    public required string Name { get; set; }

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required]
    public CampaignType Type { get; set; }

    public int? SellerId { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    public bool IsActive { get; set; } = true;

    public int? MaxUsageCount { get; set; }
    public int? MaxUsagePerUser { get; set; }

    [Range(0, 1000000)]
    public decimal? MinimumOrderAmount { get; set; }
    public int? MinimumQuantity { get; set; }

    [Range(0, 100)]
    public decimal? DiscountPercentage { get; set; }

    [Range(0, 1000000)]
    public decimal? DiscountAmount { get; set; }

    [Range(0, 1000000)]
    public decimal? MaxDiscountAmount { get; set; }

    public int? BuyQuantity { get; set; }
    public int? GetQuantity { get; set; }

    [StringLength(50)]
    public string? CouponCode { get; set; }
    public bool RequiresCouponCode { get; set; } = false;

    public CampaignScope Scope { get; set; } = CampaignScope.AllProducts;

    public int Priority { get; set; } = 0;
    public bool IsStackable { get; set; } = false;

    public List<int>? ProductIds { get; set; }
    public List<int>? ListingIds { get; set; }
    public List<int>? CategoryIds { get; set; }
}

public class UpdateCampaignDto
{
    [StringLength(200)]
    public string? Name { get; set; }

    [StringLength(1000)]
    public string? Description { get; set; }

    public CampaignType? Type { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool? IsActive { get; set; }

    public int? MaxUsageCount { get; set; }
    public int? MaxUsagePerUser { get; set; }

    public decimal? MinimumOrderAmount { get; set; }
    public int? MinimumQuantity { get; set; }

    [Range(0, 100)]
    public decimal? DiscountPercentage { get; set; }
    public decimal? DiscountAmount { get; set; }
    public decimal? MaxDiscountAmount { get; set; }

    public int? BuyQuantity { get; set; }
    public int? GetQuantity { get; set; }

    [StringLength(50)]
    public string? CouponCode { get; set; }
    public bool? RequiresCouponCode { get; set; }

    public CampaignScope? Scope { get; set; }
    public int? Priority { get; set; }
    public bool? IsStackable { get; set; }

    public List<int>? ProductIds { get; set; }
    public List<int>? ListingIds { get; set; }
    public List<int>? CategoryIds { get; set; }
}

public class ApplyCampaignDto
{
    public int? CampaignId { get; set; }
    public string? CouponCode { get; set; }
}

public class CampaignApplicationResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public int? CampaignId { get; set; }
    public string? CampaignName { get; set; }
    public CampaignType? CampaignType { get; set; }
    public decimal OriginalTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalTotal { get; set; }
    public List<CampaignItemDiscount> ItemDiscounts { get; set; } = new();
}

public class CampaignItemDiscount
{
    public int ListingId { get; set; }
    public string? ProductName { get; set; }
    public int Quantity { get; set; }
    public decimal OriginalPrice { get; set; }
    public decimal DiscountedPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public int FreeQuantity { get; set; }
}

public class CampaignUsageDto
{
    public int Id { get; set; }
    public int CampaignId { get; set; }
    public string? CampaignName { get; set; }
    public int UserId { get; set; }
    public string? UserName { get; set; }
    public int? OrderId { get; set; }
    public string? OrderNumber { get; set; }
    public decimal DiscountApplied { get; set; }
    public DateTime UsedAt { get; set; }
}

public class CampaignStatisticsDto
{
    public int CampaignId { get; set; }
    public string CampaignName { get; set; } = string.Empty;
    public int TotalUsageCount { get; set; }
    public int UniqueUsersCount { get; set; }
    public decimal TotalDiscountGiven { get; set; }
    public decimal AverageDiscountPerUse { get; set; }
    public List<DailyUsageDto> DailyUsage { get; set; } = new();
}

public class DailyUsageDto
{
    public DateTime Date { get; set; }
    public int UsageCount { get; set; }
    public decimal DiscountAmount { get; set; }
}
