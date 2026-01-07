using System.ComponentModel.DataAnnotations.Schema;
using FarmazonDemo.Models.Enums;

namespace FarmazonDemo.Models.Entities
{
    public class Order : BaseEntity
    {
        public int BuyerId { get; set; }
        public Users Buyer { get; set; } = null!;

        public OrderStatus Status { get; set; } = OrderStatus.Placed;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Total { get; set; }

        public ICollection<SellerOrder> SellerOrders { get; set; } = new List<SellerOrder>();
    }
}
