using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FarmazonDemo.Models.Enums;

namespace FarmazonDemo.Models.Entities
{
    public class Order : BaseEntity
    {
        [Required]
        [StringLength(20)]
        public required string OrderNumber { get; set; }

        public int BuyerId { get; set; }
        public Users Buyer { get; set; } = null!;

        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        [Column(TypeName = "decimal(18,2)")]
        public decimal SubTotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ShippingCost { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [StringLength(3)]
        public string Currency { get; set; } = "TRY";

        // Shipping Address (snapshot)
        public int? ShippingAddressId { get; set; }
        [StringLength(500)]
        public string? ShippingAddressSnapshot { get; set; }

        // Billing Address (snapshot)
        public int? BillingAddressId { get; set; }
        [StringLength(500)]
        public string? BillingAddressSnapshot { get; set; }

        // Notes
        [StringLength(1000)]
        public string? CustomerNote { get; set; }

        [StringLength(1000)]
        public string? AdminNote { get; set; }

        // Cancellation
        public CancellationReason? CancellationReason { get; set; }
        [StringLength(500)]
        public string? CancellationNote { get; set; }
        public DateTime? CancelledAt { get; set; }

        // Timestamps
        public DateTime? PaidAt { get; set; }
        public DateTime? ShippedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        // Navigation
        public ICollection<SellerOrder> SellerOrders { get; set; } = new List<SellerOrder>();
        public ICollection<OrderEvent> OrderEvents { get; set; } = new List<OrderEvent>();
    }
}
