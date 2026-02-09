using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Exodus.Models.Entities;

public class ProductComparison : BaseEntity
{
    [Required]
    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public Users User { get; set; } = null!;

    [StringLength(200)]
    public string? Name { get; set; }

    public ICollection<ProductComparisonItem> Items { get; set; } = new List<ProductComparisonItem>();
}

public class ProductComparisonItem : BaseEntity
{
    [Required]
    public int ComparisonId { get; set; }

    [ForeignKey(nameof(ComparisonId))]
    public ProductComparison Comparison { get; set; } = null!;

    [Required]
    public int ProductId { get; set; }

    [ForeignKey(nameof(ProductId))]
    public Product Product { get; set; } = null!;

    public int DisplayOrder { get; set; } = 0;
}
