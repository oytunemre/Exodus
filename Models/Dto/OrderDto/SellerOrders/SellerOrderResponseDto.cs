using FarmazonDemo.Models.Dto.OrderDto.SellerOrders;
using FarmazonDemo.Models.Enums;

public class SellerOrderResponseDto
{
    public int SellerOrderId { get; set; }
    public int SellerId { get; set; }
    public SellerOrderStatus Status { get; set; }
    public decimal SubTotal { get; set; }
    public List<SellerOrderItemResponseDto> Items { get; set; } = new();
    public SellerShipmentResponseDto? Shipment { get; set; }
}
