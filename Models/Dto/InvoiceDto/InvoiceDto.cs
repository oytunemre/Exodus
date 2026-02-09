using Exodus.Models.Entities;

namespace Exodus.Models.Dto
{
    public class InvoiceResponseDto
    {
        public int Id { get; set; }
        public required string InvoiceNumber { get; set; }
        public int OrderId { get; set; }
        public string? OrderNumber { get; set; }
        public InvoiceType Type { get; set; }
        public InvoiceStatus Status { get; set; }

        public required string BuyerName { get; set; }
        public string? BuyerEmail { get; set; }
        public string? BuyerAddress { get; set; }

        public string? SellerName { get; set; }
        public string? SellerAddress { get; set; }

        public decimal SubTotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string Currency { get; set; } = "TRY";

        public DateTime InvoiceDate { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? PaidDate { get; set; }

        public string? PdfUrl { get; set; }
        public string? Notes { get; set; }
    }

    public class InvoiceListItemDto
    {
        public int Id { get; set; }
        public required string InvoiceNumber { get; set; }
        public string? OrderNumber { get; set; }
        public InvoiceType Type { get; set; }
        public InvoiceStatus Status { get; set; }
        public decimal TotalAmount { get; set; }
        public string Currency { get; set; } = "TRY";
        public DateTime InvoiceDate { get; set; }
    }
}
