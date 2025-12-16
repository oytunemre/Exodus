using System.ComponentModel.DataAnnotations.Schema;

namespace FarmazonDemo.Models.Entities
{
    public class CartItem : BaseEntity
    {
        public int CartId { get; set; }
        public Cart Cart { get; set; } = null!;

        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        public int ListingId { get; set; }
        public Listing Listing { get; set; } = null!;
    }
}
