using FarmazonDemo.Models.Enums;

namespace FarmazonDemo.Models.Dto.OrderDto
{
    public class OrderResponseDto
    {
        public int OrderId { get; set; }
        public int BuyerId { get; set; }
        public OrderStatus Status { get; set; }
        public decimal Total { get; set; }
        public List<SellerOrderResponseDto> SellerOrders { get; set; } = new();
    }

}