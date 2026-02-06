using System.ComponentModel.DataAnnotations;

namespace Exodus.Models.Entities;

/// <summary>
/// Marka tanımları
/// </summary>
public class Brand : BaseEntity
{
    [Required]
    [StringLength(100)]
    public required string Name { get; set; }

    [StringLength(100)]
    public required string Slug { get; set; }

    [StringLength(1000)]
    public string? Description { get; set; }

    [StringLength(500)]
    public string? LogoUrl { get; set; }

    [StringLength(500)]
    public string? BannerUrl { get; set; }

    [StringLength(500)]
    public string? Website { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsFeatured { get; set; } = false;

    public int DisplayOrder { get; set; } = 0;

    // SEO
    [StringLength(200)]
    public string? MetaTitle { get; set; }

    [StringLength(500)]
    public string? MetaDescription { get; set; }
}
