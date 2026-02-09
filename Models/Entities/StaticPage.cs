using System.ComponentModel.DataAnnotations;

namespace Exodus.Models.Entities;

public class StaticPage : BaseEntity
{
    [Required]
    [StringLength(200)]
    public required string Title { get; set; }

    [Required]
    [StringLength(200)]
    public required string Slug { get; set; } // URL-friendly identifier (e.g., "about-us", "privacy-policy")

    [Required]
    public required string Content { get; set; } // HTML content

    // SEO
    [StringLength(200)]
    public string? MetaTitle { get; set; }

    [StringLength(500)]
    public string? MetaDescription { get; set; }

    [StringLength(500)]
    public string? MetaKeywords { get; set; }

    // Display
    public bool IsPublished { get; set; } = true;
    public bool ShowInFooter { get; set; } = false;
    public bool ShowInHeader { get; set; } = false;
    public int DisplayOrder { get; set; } = 0;

    // Template/Category
    public StaticPageType PageType { get; set; } = StaticPageType.General;

    // Timestamps
    public DateTime? PublishedAt { get; set; }
    public int? LastEditedByUserId { get; set; }
}

public enum StaticPageType
{
    General,
    Legal, // Privacy, Terms, etc.
    Help, // FAQ, How it works
    About, // About us, Contact
    Landing // Landing pages
}
