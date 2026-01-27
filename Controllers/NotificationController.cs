using FarmazonDemo.Models.Dto;
using FarmazonDemo.Services.Notifications;
using FarmazonDemo.Services.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FarmazonDemo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        /// <summary>
        /// Get user's notifications
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<NotificationListDto>> GetNotifications(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] bool? isRead = null)
        {
            var userId = GetCurrentUserId();
            var notifications = await _notificationService.GetUserNotificationsAsync(userId, page, pageSize, isRead);
            return Ok(notifications);
        }

        /// <summary>
        /// Get notification by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<NotificationResponseDto>> GetNotification(int id)
        {
            var userId = GetCurrentUserId();
            var notification = await _notificationService.GetByIdAsync(userId, id);
            return Ok(notification);
        }

        /// <summary>
        /// Get unread count
        /// </summary>
        [HttpGet("unread-count")]
        public async Task<ActionResult> GetUnreadCount()
        {
            var userId = GetCurrentUserId();
            var count = await _notificationService.GetUnreadCountAsync(userId);
            return Ok(new { UnreadCount = count });
        }

        /// <summary>
        /// Mark notification as read
        /// </summary>
        [HttpPatch("{id}/read")]
        public async Task<ActionResult> MarkAsRead(int id)
        {
            var userId = GetCurrentUserId();
            await _notificationService.MarkAsReadAsync(userId, id);
            return Ok(new { Message = "Notification marked as read" });
        }

        /// <summary>
        /// Mark all notifications as read
        /// </summary>
        [HttpPatch("read-all")]
        public async Task<ActionResult> MarkAllAsRead()
        {
            var userId = GetCurrentUserId();
            await _notificationService.MarkAllAsReadAsync(userId);
            return Ok(new { Message = "All notifications marked as read" });
        }

        /// <summary>
        /// Delete notification
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteNotification(int id)
        {
            var userId = GetCurrentUserId();
            await _notificationService.DeleteAsync(userId, id);
            return NoContent();
        }

        /// <summary>
        /// Delete all read notifications
        /// </summary>
        [HttpDelete("read")]
        public async Task<ActionResult> DeleteAllRead()
        {
            var userId = GetCurrentUserId();
            await _notificationService.DeleteAllReadAsync(userId);
            return Ok(new { Message = "All read notifications deleted" });
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                throw new UnauthorizedException("Invalid user token");
            return userId;
        }
    }
}
