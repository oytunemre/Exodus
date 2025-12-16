using System.ComponentModel.DataAnnotations;

namespace FarmazonDemo.Models.Entities
{
    public class ProductBarcode : BaseEntity
    {
        [MaxLength(100)]
        public required string Barcode { get; set; } = string.Empty;

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
    }
}
