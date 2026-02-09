using System.ComponentModel.DataAnnotations;

namespace Exodus.Models.Entities;

/// <summary>
/// Ürün özellik tanımları (Renk, Beden, Kapasite, etc.)
/// </summary>
public class ProductAttribute : BaseEntity
{
    [Required]
    [StringLength(100)]
    public required string Name { get; set; } // Renk, Beden, Kapasite

    [StringLength(50)]
    public string? Code { get; set; } // COLOR, SIZE, CAPACITY

    public AttributeType Type { get; set; } = AttributeType.Select;

    public bool IsRequired { get; set; } = false;

    public bool IsFilterable { get; set; } = true; // Filtrelerde gösterilsin mi

    public bool IsVisibleOnProduct { get; set; } = true;

    public int DisplayOrder { get; set; } = 0;

    public bool IsActive { get; set; } = true;

    // Applicable categories (null = all)
    [StringLength(500)]
    public string? ApplicableCategoryIds { get; set; }

    public ICollection<ProductAttributeValue> Values { get; set; } = new List<ProductAttributeValue>();
}

/// <summary>
/// Özellik değerleri (Kırmızı, Mavi, S, M, L, XL, etc.)
/// </summary>
public class ProductAttributeValue : BaseEntity
{
    public int AttributeId { get; set; }
    public ProductAttribute Attribute { get; set; } = null!;

    [Required]
    [StringLength(100)]
    public required string Value { get; set; } // Kırmızı, M, 128GB

    [StringLength(50)]
    public string? Code { get; set; } // RED, M, 128GB

    // For color type
    [StringLength(7)]
    public string? ColorHex { get; set; } // #FF0000

    // For image type
    [StringLength(500)]
    public string? ImageUrl { get; set; }

    public int DisplayOrder { get; set; } = 0;

    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Ürün-Özellik ilişkisi
/// </summary>
public class ProductAttributeMapping : BaseEntity
{
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int AttributeId { get; set; }
    public ProductAttribute Attribute { get; set; } = null!;

    public int AttributeValueId { get; set; }
    public ProductAttributeValue AttributeValue { get; set; } = null!;
}

public enum AttributeType
{
    Select = 0,      // Dropdown
    Radio = 1,       // Radio buttons
    Checkbox = 2,    // Checkboxes (multiple select)
    Color = 3,       // Color picker
    Text = 4,        // Free text
    Number = 5       // Numeric input
}
