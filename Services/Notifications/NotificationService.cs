using FarmazonDemo.Data;
using FarmazonDemo.Models.Dto;
using FarmazonDemo.Models.Entities;
using FarmazonDemo.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace FarmazonDemo.Services.Notifications
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;

        public NotificationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<NotificationListDto> GetUserNotificationsAsync(int userId, int page = 1, int pageSize = 20, bool? isRead = null)
        {
            var query = _context.Notifications
                .Where(n => n.UserId == userId);

            if (isRead.HasValue)
                query = query.Where(n => n.IsRead == isRead.Value);

            var totalCount = await query.CountAsync();
            var unreadCount = await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);

            var notifications = await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(n => new NotificationResponseDto
                {
                    Id = n.Id,
                    Title = n.Title,
                    Message = n.Message,
                    Type = n.Type,
                    IsRead = n.IsRead,
                    ReadAt = n.ReadAt,
                    ActionUrl = n.ActionUrl,
                    Icon = n.Icon,
                    CreatedAt = n.CreatedAt
                })
                .ToListAsync();

            return new NotificationListDto
            {
                Items = notifications,
                TotalCount = totalCount,
                UnreadCount = unreadCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<NotificationResponseDto> GetByIdAsync(int userId, int notificationId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

            if (notification == null)
                throw new NotFoundException("Notification not found");

            return new NotificationResponseDto
            {
                Id = notification.Id,
                Title = notification.Title,
                Message = notification.Message,
                Type = notification.Type,
                IsRead = notification.IsRead,
                ReadAt = notification.ReadAt,
                ActionUrl = notification.ActionUrl,
                Icon = notification.Icon,
                CreatedAt = notification.CreatedAt
            };
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            return await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        public async Task MarkAsReadAsync(int userId, int notificationId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

            if (notification == null)
                throw new NotFoundException("Notification not found");

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkAllAsReadAsync(int userId)
        {
            var unreadNotifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            var now = DateTime.UtcNow;
            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
                notification.ReadAt = now;
            }

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int userId, int notificationId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

            if (notification == null)
                throw new NotFoundException("Notification not found");

            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAllReadAsync(int userId)
        {
            var readNotifications = await _context.Notifications
                .Where(n => n.UserId == userId && n.IsRead)
                .ToListAsync();

            _context.Notifications.RemoveRange(readNotifications);
            await _context.SaveChangesAsync();
        }

        public async Task SendAsync(CreateNotificationDto dto)
        {
            var notification = new Notification
            {
                UserId = dto.UserId,
                Title = dto.Title,
                Message = dto.Message,
                Type = dto.Type,
                ActionUrl = dto.ActionUrl,
                Icon = dto.Icon ?? GetDefaultIcon(dto.Type),
                RelatedEntityType = dto.RelatedEntityType,
                RelatedEntityId = dto.RelatedEntityId
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        public async Task SendOrderUpdateAsync(int userId, int orderId, string title, string message)
        {
            await SendAsync(new CreateNotificationDto
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = NotificationType.OrderUpdate,
                ActionUrl = $"/orders/{orderId}",
                RelatedEntityType = "Order",
                RelatedEntityId = orderId
            });
        }

        public async Task SendPaymentUpdateAsync(int userId, int orderId, string title, string message)
        {
            await SendAsync(new CreateNotificationDto
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = NotificationType.PaymentUpdate,
                ActionUrl = $"/orders/{orderId}",
                RelatedEntityType = "Order",
                RelatedEntityId = orderId
            });
        }

        public async Task SendShipmentUpdateAsync(int userId, int shipmentId, string title, string message)
        {
            await SendAsync(new CreateNotificationDto
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = NotificationType.ShipmentUpdate,
                ActionUrl = $"/shipments/{shipmentId}",
                RelatedEntityType = "Shipment",
                RelatedEntityId = shipmentId
            });
        }

        public async Task SendSystemNotificationAsync(int userId, string title, string message)
        {
            await SendAsync(new CreateNotificationDto
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = NotificationType.System
            });
        }

        public async Task SendBulkAsync(IEnumerable<int> userIds, string title, string message, NotificationType type)
        {
            var notifications = userIds.Select(userId => new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                Icon = GetDefaultIcon(type)
            });

            _context.Notifications.AddRange(notifications);
            await _context.SaveChangesAsync();
        }

        private static string GetDefaultIcon(NotificationType type)
        {
            return type switch
            {
                NotificationType.Info => "info-circle",
                NotificationType.Success => "check-circle",
                NotificationType.Warning => "exclamation-triangle",
                NotificationType.Error => "times-circle",
                NotificationType.OrderUpdate => "shopping-bag",
                NotificationType.PaymentUpdate => "credit-card",
                NotificationType.ShipmentUpdate => "truck",
                NotificationType.PriceAlert => "tag",
                NotificationType.StockAlert => "box",
                NotificationType.Promotion => "gift",
                NotificationType.System => "cog",
                _ => "bell"
            };
        }
    }
}
