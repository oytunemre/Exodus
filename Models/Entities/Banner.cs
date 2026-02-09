using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Exodus.Models.Entities;

public class Banner : BaseEntity
{
    [Required]
    [StringLength(200)]
    public required string Title { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    [StringLength(500)]
    public required string ImageUrl { get; set; }

    [StringLength(500)]
    public string? MobileImageUrl { get; set; }

    [StringLength(500)]
    public string? TargetUrl { get; set; }

    public BannerPosition Position { get; set; } = BannerPosition.HomeSlider;

    public int DisplayOrder { get; set; } = 0;

    public bool IsActive { get; set; } = true;

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    // Click tracking
    public int ClickCount { get; set; } = 0;

    public int ViewCount { get; set; } = 0;
}

public enum BannerPosition
{
    HomeSlider = 0,
    HomeBanner = 1,
    CategoryBanner = 2,
    ProductBanner = 3,
    CheckoutBanner = 4
}
