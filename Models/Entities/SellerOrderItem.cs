using System.ComponentModel.DataAnnotations.Schema;

namespace Exodus.Models.Entities
{
    public class SellerOrderItem : BaseEntity
    {
        public int SellerOrderId { get; set; }
        public SellerOrder SellerOrder { get; set; } = null!;

        public int ListingId { get; set; }
        public Listing Listing { get; set; } = null!;

        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal LineTotal { get; set; }
    }
}
