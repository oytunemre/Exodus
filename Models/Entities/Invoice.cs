using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmazonDemo.Models.Entities
{
    public class Invoice : BaseEntity
    {
        [Required]
        [StringLength(30)]
        public required string InvoiceNumber { get; set; }

        public int OrderId { get; set; }

        [ForeignKey("OrderId")]
        public Order Order { get; set; } = null!;

        public int? SellerOrderId { get; set; }

        [ForeignKey("SellerOrderId")]
        public SellerOrder? SellerOrder { get; set; }

        public InvoiceType Type { get; set; } = InvoiceType.Sale;

        public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

        // Buyer Info (snapshot)
        [Required]
        [StringLength(200)]
        public required string BuyerName { get; set; }

        [StringLength(200)]
        public string? BuyerEmail { get; set; }

        [StringLength(20)]
        public string? BuyerPhone { get; set; }

        [StringLength(500)]
        public string? BuyerAddress { get; set; }

        [StringLength(20)]
        public string? BuyerTaxNumber { get; set; }

        // Seller Info (snapshot)
        [StringLength(200)]
        public string? SellerName { get; set; }

        [StringLength(500)]
        public string? SellerAddress { get; set; }

        [StringLength(20)]
        public string? SellerTaxNumber { get; set; }

        // Amounts
        [Column(TypeName = "decimal(18,2)")]
        public decimal SubTotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [StringLength(3)]
        public string Currency { get; set; } = "TRY";

        // Dates
        public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;
        public DateTime? DueDate { get; set; }
        public DateTime? PaidDate { get; set; }

        // PDF
        [StringLength(500)]
        public string? PdfUrl { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }
    }

    public enum InvoiceType
    {
        Sale,       // Satış faturası
        Refund,     // İade faturası
        Proforma    // Proforma fatura
    }

    public enum InvoiceStatus
    {
        Draft,
        Issued,
        Sent,
        Paid,
        Cancelled,
        Refunded
    }
}
