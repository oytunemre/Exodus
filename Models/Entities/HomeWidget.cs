using System.ComponentModel.DataAnnotations;

namespace FarmazonDemo.Models.Entities;

/// <summary>
/// Anasayfa widget/bölüm tanımları
/// </summary>
public class HomeWidget : BaseEntity
{
    [Required]
    [StringLength(100)]
    public required string Name { get; set; }

    [StringLength(50)]
    public string? Code { get; set; }

    public HomeWidgetType Type { get; set; }

    [StringLength(200)]
    public string? Title { get; set; }

    [StringLength(500)]
    public string? Subtitle { get; set; }

    // Content (JSON based on type)
    public string? Configuration { get; set; }

    // For product widgets
    public int? CategoryId { get; set; }
    public int? BrandId { get; set; }
    public int? CampaignId { get; set; }

    [StringLength(500)]
    public string? ProductIds { get; set; } // Manual product selection

    public int ItemCount { get; set; } = 10;

    // Display settings
    public HomeWidgetPosition Position { get; set; } = HomeWidgetPosition.Main;
    public int DisplayOrder { get; set; } = 0;

    public bool IsActive { get; set; } = true;

    // Visibility
    public bool ShowOnMobile { get; set; } = true;
    public bool ShowOnDesktop { get; set; } = true;

    // Scheduling
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public enum HomeWidgetType
{
    Banner = 0,           // Tek banner
    BannerSlider = 1,     // Banner slider
    ProductSlider = 2,    // Ürün slider
    ProductGrid = 3,      // Ürün grid
    CategorySlider = 4,   // Kategori slider
    BrandSlider = 5,      // Marka slider
    Countdown = 6,        // Geri sayım
    Html = 7,             // Custom HTML
    Newsletter = 8,       // Newsletter signup
    Testimonials = 9,     // Müşteri yorumları
    FeaturedSellers = 10  // Öne çıkan satıcılar
}

public enum HomeWidgetPosition
{
    Top = 0,        // Üst (banner sonrası)
    Main = 1,       // Ana içerik
    Sidebar = 2,    // Yan panel
    Bottom = 3,     // Alt
    Footer = 4      // Footer üstü
}
