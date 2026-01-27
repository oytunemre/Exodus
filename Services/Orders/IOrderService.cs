using FarmazonDemo.Models.Dto;
using FarmazonDemo.Models.Dto.OrderDto;
using FarmazonDemo.Models.Enums;

namespace FarmazonDemo.Services.Orders
{
    public interface IOrderService
    {
        // Checkout
        Task<OrderDetailResponseDto> CheckoutAsync(int userId, CreateOrderDto dto);

        // Order Queries
        Task<OrderDetailResponseDto> GetByIdAsync(int userId, int orderId);
        Task<OrderDetailResponseDto> GetByOrderNumberAsync(int userId, string orderNumber);
        Task<OrderListResponseDto> GetUserOrdersAsync(int userId, OrderStatus? status = null, int page = 1, int pageSize = 10);

        // Order Status
        Task<OrderDetailResponseDto> UpdateStatusAsync(int orderId, OrderStatus newStatus, int? userId = null, string? note = null);
        Task<OrderDetailResponseDto> CancelOrderAsync(int userId, int orderId, CancelOrderDto dto);

        // Refunds
        Task<RefundResponseDto> RequestRefundAsync(int userId, int orderId, RefundRequestDto dto);
        Task<RefundResponseDto> ProcessRefundAsync(int refundId, bool approve, int adminUserId, string? note = null);

        // Seller Orders
        Task<List<SellerOrderDto>> GetSellerOrdersAsync(int sellerId, OrderStatus? status = null, int page = 1, int pageSize = 20);
        Task UpdateSellerOrderStatusAsync(int sellerId, int sellerOrderId, OrderStatus newStatus);

        // Helpers
        Task AddOrderEventAsync(int orderId, OrderStatus status, string title, string? description = null, int? userId = null, string? userType = null);
        string GenerateOrderNumber();
    }
}
