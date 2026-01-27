using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FarmazonDemo.Models.Enums;

namespace FarmazonDemo.Models.Entities
{
    public class Refund : BaseEntity
    {
        [Required]
        [StringLength(20)]
        public required string RefundNumber { get; set; }

        public int OrderId { get; set; }

        [ForeignKey("OrderId")]
        public Order Order { get; set; } = null!;

        public int? SellerOrderId { get; set; }

        [ForeignKey("SellerOrderId")]
        public SellerOrder? SellerOrder { get; set; }

        public RefundStatus Status { get; set; } = RefundStatus.Pending;

        public RefundType Type { get; set; } = RefundType.Full;

        [Required]
        [StringLength(500)]
        public required string Reason { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [StringLength(3)]
        public string Currency { get; set; } = "TRY";

        // Refund method
        public RefundMethod Method { get; set; } = RefundMethod.OriginalPayment;

        [StringLength(100)]
        public string? ExternalReference { get; set; }

        // Processing
        public int? ProcessedByUserId { get; set; }
        public DateTime? ProcessedAt { get; set; }

        [StringLength(500)]
        public string? AdminNote { get; set; }

        // If rejected
        [StringLength(500)]
        public string? RejectionReason { get; set; }
    }

    public enum RefundType
    {
        Full,
        Partial
    }

    public enum RefundMethod
    {
        OriginalPayment,
        BankTransfer,
        StoreCredit,
        Cash
    }
}
