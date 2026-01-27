using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmazonDemo.Models.Entities
{
    public class ProductImage : BaseEntity
    {
        [Required]
        [StringLength(500)]
        public required string Url { get; set; }

        [StringLength(500)]
        public string? ThumbnailUrl { get; set; }

        [StringLength(255)]
        public string? AltText { get; set; }

        public int DisplayOrder { get; set; } = 0;

        public bool IsPrimary { get; set; } = false;

        public long FileSizeBytes { get; set; }

        [StringLength(50)]
        public string? ContentType { get; set; }

        // Foreign key to Product
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public Product Product { get; set; } = null!;
    }
}
