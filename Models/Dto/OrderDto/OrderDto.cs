using System.ComponentModel.DataAnnotations;
using FarmazonDemo.Models.Entities;
using FarmazonDemo.Models.Enums;

namespace FarmazonDemo.Models.Dto
{
    public class CreateOrderDto
    {
        public int ShippingAddressId { get; set; }
        public int? BillingAddressId { get; set; }

        [StringLength(1000)]
        public string? CustomerNote { get; set; }

        public string? CouponCode { get; set; }
    }

    public class OrderDetailResponseDto
    {
        public int Id { get; set; }
        public required string OrderNumber { get; set; }
        public OrderStatus Status { get; set; }
        public decimal SubTotal { get; set; }
        public decimal ShippingCost { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string Currency { get; set; } = "TRY";

        public string? ShippingAddress { get; set; }
        public string? BillingAddress { get; set; }
        public string? CustomerNote { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? PaidAt { get; set; }
        public DateTime? ShippedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }

        public List<SellerOrderDto> SellerOrders { get; set; } = new();
        public List<OrderEventDto> Events { get; set; } = new();
    }

    public class SellerOrderDto
    {
        public int Id { get; set; }
        public int SellerId { get; set; }
        public string SellerName { get; set; } = string.Empty;
        public OrderStatus Status { get; set; }
        public decimal Total { get; set; }
        public List<OrderItemDto> Items { get; set; } = new();
        public ShipmentInfoDto? Shipment { get; set; }
    }

    public class OrderItemDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public required string ProductName { get; set; }
        public string? ProductImage { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public class ShipmentInfoDto
    {
        public int Id { get; set; }
        public string? Carrier { get; set; }
        public string? TrackingNumber { get; set; }
        public string? TrackingUrl { get; set; }
        public DateTime? ShippedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
    }

    public class OrderEventDto
    {
        public int Id { get; set; }
        public OrderStatus Status { get; set; }
        public required string Title { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class OrderListItemDto
    {
        public int Id { get; set; }
        public required string OrderNumber { get; set; }
        public OrderStatus Status { get; set; }
        public decimal TotalAmount { get; set; }
        public string Currency { get; set; } = "TRY";
        public int ItemCount { get; set; }
        public string? FirstProductImage { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class OrderListResponseDto
    {
        public List<OrderListItemDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    public class CancelOrderDto
    {
        [Required]
        public CancellationReason Reason { get; set; }

        [StringLength(500)]
        public string? Note { get; set; }
    }

    public class RefundRequestDto
    {
        public int? SellerOrderId { get; set; }

        [Required]
        [StringLength(500)]
        public required string Reason { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        public decimal? Amount { get; set; } // For partial refunds
    }

    public class RefundResponseDto
    {
        public int Id { get; set; }
        public required string RefundNumber { get; set; }
        public RefundStatus Status { get; set; }
        public RefundType Type { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "TRY";
        public required string Reason { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
    }
}
