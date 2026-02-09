using Exodus.Models.Enums;

namespace Exodus.Models.Dto.SellerDto
{
    public class SellerOrderListDto
    {
        public int SellerOrderId { get; set; }
        public int OrderId { get; set; }
        public int SellerId { get; set; }

        public SellerOrderStatus Status { get; set; }
        public decimal SubTotal { get; set; }

        public List<SellerOrderItemDto> Items { get; set; } = new();
        public SellerShipmentDto? Shipment { get; set; }
    }
}
