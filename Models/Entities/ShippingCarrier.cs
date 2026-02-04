using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmazonDemo.Models.Entities;

public class ShippingCarrier : BaseEntity
{
    [Required]
    [StringLength(100)]
    public required string Name { get; set; }

    [StringLength(50)]
    public string? Code { get; set; } // e.g., "YURTICI", "MNG", "ARAS"

    [StringLength(500)]
    public string? LogoUrl { get; set; }

    [StringLength(500)]
    public string? TrackingUrlTemplate { get; set; } // e.g., "https://tracking.yurtici.com.tr/?code={tracking}"

    [StringLength(500)]
    public string? Website { get; set; }

    [StringLength(20)]
    public string? Phone { get; set; }

    public bool IsActive { get; set; } = true;

    public bool SupportsApi { get; set; } = false;

    [StringLength(500)]
    public string? ApiEndpoint { get; set; }

    [StringLength(200)]
    public string? ApiKey { get; set; }

    // Default shipping rates
    [Column(TypeName = "decimal(18,2)")]
    public decimal? DefaultRate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? FreeShippingThreshold { get; set; }

    public int DisplayOrder { get; set; } = 0;
}
