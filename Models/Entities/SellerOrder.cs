using System.ComponentModel.DataAnnotations.Schema;
using FarmazonDemo.Models.Enums;

namespace FarmazonDemo.Models.Entities
{
    public class SellerOrder : BaseEntity
    {
        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;

        public int SellerId { get; set; }
        public Users Seller { get; set; } = null!;

        public SellerOrderStatus Status { get; set; } = SellerOrderStatus.Placed;

        [Column(TypeName = "decimal(18,2)")]
        public decimal SubTotal { get; set; }

        public ICollection<SellerOrderItem> Items { get; set; } = new List<SellerOrderItem>();

        public Shipment? Shipment { get; set; }
    }
}
