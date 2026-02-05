using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmazonDemo.Models.Entities;

/// <summary>
/// Vergi oranları
/// </summary>
public class TaxRate : BaseEntity
{
    [Required]
    [StringLength(100)]
    public required string Name { get; set; } // KDV %18, KDV %8, etc.

    [StringLength(20)]
    public string? Code { get; set; } // KDV18, KDV8, etc.

    [Column(TypeName = "decimal(5,2)")]
    public decimal Rate { get; set; } // 18.00, 8.00, etc.

    public bool IsDefault { get; set; } = false;

    public bool IsActive { get; set; } = true;

    // Applicable to specific categories
    public bool AppliesToAllCategories { get; set; } = true;

    [StringLength(500)]
    public string? ApplicableCategoryIds { get; set; } // Comma-separated

    public int DisplayOrder { get; set; } = 0;
}
