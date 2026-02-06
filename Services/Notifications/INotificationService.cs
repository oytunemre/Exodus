using Exodus.Models.Dto;
using Exodus.Models.Entities;

namespace Exodus.Services.Notifications
{
    public interface INotificationService
    {
        Task<NotificationListDto> GetUserNotificationsAsync(int userId, int page = 1, int pageSize = 20, bool? isRead = null);
        Task<NotificationResponseDto> GetByIdAsync(int userId, int notificationId);
        Task<int> GetUnreadCountAsync(int userId);
        Task MarkAsReadAsync(int userId, int notificationId);
        Task MarkAllAsReadAsync(int userId);
        Task DeleteAsync(int userId, int notificationId);
        Task DeleteAllReadAsync(int userId);

        // Send notifications
        Task SendAsync(CreateNotificationDto dto);
        Task SendOrderUpdateAsync(int userId, int orderId, string title, string message);
        Task SendPaymentUpdateAsync(int userId, int orderId, string title, string message);
        Task SendShipmentUpdateAsync(int userId, int shipmentId, string title, string message);
        Task SendSystemNotificationAsync(int userId, string title, string message);
        Task SendBulkAsync(IEnumerable<int> userIds, string title, string message, NotificationType type);
    }
}
