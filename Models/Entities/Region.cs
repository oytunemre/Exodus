using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmazonDemo.Models.Entities;

/// <summary>
/// Şehir/İlçe/Bölge tanımları
/// </summary>
public class Region : BaseEntity
{
    [Required]
    [StringLength(100)]
    public required string Name { get; set; }

    [StringLength(10)]
    public string? Code { get; set; } // 34, 06, etc.

    public RegionType Type { get; set; }

    public int? ParentId { get; set; }
    public Region? Parent { get; set; }

    public bool IsActive { get; set; } = true;

    // Shipping zone
    public int? ShippingZoneId { get; set; }

    public int DisplayOrder { get; set; } = 0;

    public ICollection<Region> Children { get; set; } = new List<Region>();
}

public enum RegionType
{
    Country = 0,
    Province = 1,  // İl
    District = 2,  // İlçe
    Neighborhood = 3 // Mahalle
}

/// <summary>
/// Kargo bölgeleri (farklı fiyatlandırma için)
/// </summary>
public class ShippingZone : BaseEntity
{
    [Required]
    [StringLength(100)]
    public required string Name { get; set; } // İstanbul İçi, Marmara, Anadolu, etc.

    [StringLength(500)]
    public string? Description { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal BaseShippingCost { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? FreeShippingThreshold { get; set; }

    public int EstimatedDeliveryDays { get; set; } = 3;

    public bool IsActive { get; set; } = true;

    public int DisplayOrder { get; set; } = 0;
}
