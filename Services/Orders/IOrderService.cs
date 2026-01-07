using FarmazonDemo.Models.Dto.OrderDto;

namespace FarmazonDemo.Services.Orders
{
    public interface IOrderService
    {
        Task<OrderResponseDto> CheckoutAsync(int userId);
        Task<OrderResponseDto> GetByIdAsync(int orderId);
        Task<List<OrderResponseDto>> GetByUserAsync(int userId);
    }
}
