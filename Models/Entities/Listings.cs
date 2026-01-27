using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FarmazonDemo.Models.Enums;

namespace FarmazonDemo.Models.Entities
{
    public class Listing : BaseEntity
    {
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public int SellerId { get; set; }
        public Users Seller { get; set; } = null!;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        // Stock Management
        public int StockQuantity { get; set; } = 0;

        public int LowStockThreshold { get; set; } = 5;

        public bool TrackInventory { get; set; } = true;

        public StockStatus StockStatus { get; set; } = StockStatus.InStock;

        [StringLength(50)]
        public string? SKU { get; set; }

        public ListingCondition Condition { get; set; } = ListingCondition.New;

        public bool IsActive { get; set; } = true;

        // Computed property (not stored in DB)
        [NotMapped]
        public bool IsLowStock => TrackInventory && StockQuantity > 0 && StockQuantity <= LowStockThreshold;
    }
}
