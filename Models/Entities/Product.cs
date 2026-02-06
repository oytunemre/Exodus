using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Exodus.Models.Entities
{
    public class Product : BaseEntity
    {
        [Required]
        [StringLength(200)]
        public required string ProductName { get; set; } = string.Empty;

        [StringLength(2000)]
        public required string ProductDescription { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Brand { get; set; }

        [StringLength(100)]
        public string? Manufacturer { get; set; }

        // Category relationship
        public int? CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public Category? Category { get; set; }

        // Navigation properties
        public ICollection<ProductBarcode> Barcodes { get; set; } = new List<ProductBarcode>();
        public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    }
}
