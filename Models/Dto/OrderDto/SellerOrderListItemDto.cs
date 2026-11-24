using Exodus.Models.Enums;

namespace Exodus.Models.Dto.OrderDto
{
    public class SellerOrderListItemDto
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int SellerId { get; set; }
        public string SellerName { get; set; } = string.Empty;
        public OrderStatus Status { get; set; }
        public decimal Total { get; set; }
        public List<SellerOrderLineDto> Items { get; set; } = new();
        public SellerOrderShipmentDto? Shipment { get; set; }
    }

    public class SellerOrderLineDto
    {
        public int Id { get; set; }
        public int ListingId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public class SellerOrderShipmentDto
    {
        public int Id { get; set; }
        public string? Carrier { get; set; }
        public string? TrackingNumber { get; set; }
        public DateTime? ShippedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
    }
}
