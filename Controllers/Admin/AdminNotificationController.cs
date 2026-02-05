using FarmazonDemo.Data;
using FarmazonDemo.Models.Entities;
using FarmazonDemo.Models.Enums;
using FarmazonDemo.Services.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace FarmazonDemo.Controllers.Admin;

[Route("api/admin/notifications")]
[ApiController]
[Authorize(Policy = "AdminOnly")]
public class AdminNotificationController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public AdminNotificationController(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Get all notifications with filtering
    /// </summary>
    [HttpGet]
    public async Task<ActionResult> GetNotifications(
        [FromQuery] NotificationType? type = null,
        [FromQuery] bool? isRead = null,
        [FromQuery] int? userId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = _db.Notifications.AsQueryable();

        if (type.HasValue)
            query = query.Where(n => n.Type == type.Value);

        if (isRead.HasValue)
            query = query.Where(n => n.IsRead == isRead.Value);

        if (userId.HasValue)
            query = query.Where(n => n.UserId == userId.Value);

        if (fromDate.HasValue)
            query = query.Where(n => n.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(n => n.CreatedAt <= toDate.Value);

        var totalCount = await query.CountAsync();

        var notifications = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new
            {
                n.Id,
                n.UserId,
                UserName = _db.Users.Where(u => u.Id == n.UserId).Select(u => u.Name).FirstOrDefault(),
                n.Title,
                n.Message,
                n.Type,
                n.IsRead,
                n.ReadAt,
                n.CreatedAt
            })
            .ToListAsync();

        return Ok(new
        {
            Items = notifications,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
    }

    /// <summary>
    /// Send notification to a specific user
    /// </summary>
    [HttpPost("send")]
    public async Task<ActionResult> SendNotification([FromBody] SendNotificationDto dto)
    {
        var user = await _db.Users.FindAsync(dto.UserId);
        if (user == null)
            throw new NotFoundException("User not found");

        var notification = new Notification
        {
            UserId = dto.UserId,
            Title = dto.Title,
            Message = dto.Message,
            Type = dto.Type,
            ActionUrl = dto.ActionUrl
        };

        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            Message = "Notification sent successfully",
            NotificationId = notification.Id
        });
    }

    /// <summary>
    /// Send bulk notification to multiple users
    /// </summary>
    [HttpPost("send-bulk")]
    public async Task<ActionResult> SendBulkNotification([FromBody] SendBulkNotificationDto dto)
    {
        var userIds = dto.UserIds;

        // If target group specified, get user IDs
        if (dto.TargetGroup.HasValue)
        {
            userIds = dto.TargetGroup.Value switch
            {
                NotificationTargetGroup.AllUsers => await _db.Users.Select(u => u.Id).ToListAsync(),
                NotificationTargetGroup.AllCustomers => await _db.Users
                    .Where(u => u.Role == UserRole.Customer)
                    .Select(u => u.Id)
                    .ToListAsync(),
                NotificationTargetGroup.AllSellers => await _db.Users
                    .Where(u => u.Role == UserRole.Seller)
                    .Select(u => u.Id)
                    .ToListAsync(),
                NotificationTargetGroup.ActiveUsers => await _db.Users
                    .Where(u => (!u.LockoutEndTime.HasValue || u.LockoutEndTime <= DateTime.UtcNow) && u.LastLoginAt >= DateTime.UtcNow.AddDays(-30))
                    .Select(u => u.Id)
                    .ToListAsync(),
                NotificationTargetGroup.VerifiedSellers => await _db.SellerProfiles
                    .Where(p => p.VerificationStatus == SellerVerificationStatus.Approved)
                    .Select(p => p.UserId)
                    .ToListAsync(),
                _ => userIds ?? new List<int>()
            };
        }

        if (userIds == null || !userIds.Any())
            throw new BadRequestException("No users to notify");

        var notifications = userIds.Select(userId => new Notification
        {
            UserId = userId,
            Title = dto.Title,
            Message = dto.Message,
            Type = dto.Type,
            ActionUrl = dto.ActionUrl
        }).ToList();

        _db.Notifications.AddRange(notifications);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            Message = "Bulk notification sent successfully",
            RecipientCount = notifications.Count
        });
    }

    /// <summary>
    /// Send notification to users based on filter
    /// </summary>
    [HttpPost("send-filtered")]
    public async Task<ActionResult> SendFilteredNotification([FromBody] SendFilteredNotificationDto dto)
    {
        var query = _db.Users.AsQueryable();

        // Apply filters
        if (dto.Role.HasValue)
            query = query.Where(u => u.Role == dto.Role.Value);

        if (dto.IsActive.HasValue)
            query = query.Where(u => dto.IsActive.Value ? (!u.LockoutEndTime.HasValue || u.LockoutEndTime <= DateTime.UtcNow) : (u.LockoutEndTime.HasValue && u.LockoutEndTime > DateTime.UtcNow));

        if (dto.IsEmailVerified.HasValue)
            query = query.Where(u => u.EmailVerified == dto.IsEmailVerified.Value);

        if (dto.RegisteredAfter.HasValue)
            query = query.Where(u => u.CreatedAt >= dto.RegisteredAfter.Value);

        if (dto.RegisteredBefore.HasValue)
            query = query.Where(u => u.CreatedAt <= dto.RegisteredBefore.Value);

        if (dto.LastLoginAfter.HasValue)
            query = query.Where(u => u.LastLoginAt >= dto.LastLoginAfter.Value);

        if (dto.HasOrders.HasValue)
        {
            var userIdsWithOrders = await _db.Orders.Select(o => o.BuyerId).Distinct().ToListAsync();
            if (dto.HasOrders.Value)
                query = query.Where(u => userIdsWithOrders.Contains(u.Id));
            else
                query = query.Where(u => !userIdsWithOrders.Contains(u.Id));
        }

        var userIds = await query.Select(u => u.Id).ToListAsync();

        if (!userIds.Any())
            return Ok(new { Message = "No users match the filter criteria", RecipientCount = 0 });

        var notifications = userIds.Select(userId => new Notification
        {
            UserId = userId,
            Title = dto.Title,
            Message = dto.Message,
            Type = dto.Type,
            ActionUrl = dto.ActionUrl
        }).ToList();

        _db.Notifications.AddRange(notifications);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            Message = "Filtered notification sent successfully",
            RecipientCount = notifications.Count
        });
    }

    /// <summary>
    /// Delete a notification
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeleteNotification(int id)
    {
        var notification = await _db.Notifications.FindAsync(id);
        if (notification == null)
            throw new NotFoundException("Notification not found");

        _db.Notifications.Remove(notification);
        await _db.SaveChangesAsync();

        return Ok(new { Message = "Notification deleted", NotificationId = id });
    }

    /// <summary>
    /// Delete bulk notifications
    /// </summary>
    [HttpPost("delete-bulk")]
    public async Task<ActionResult> DeleteBulkNotifications([FromBody] DeleteBulkNotificationsDto dto)
    {
        var notifications = await _db.Notifications
            .Where(n => dto.NotificationIds.Contains(n.Id))
            .ToListAsync();

        if (!notifications.Any())
            throw new NotFoundException("No notifications found");

        _db.Notifications.RemoveRange(notifications);
        await _db.SaveChangesAsync();

        return Ok(new { Message = "Notifications deleted", DeletedCount = notifications.Count });
    }

    /// <summary>
    /// Get notification statistics
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult> GetStatistics(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
        var to = toDate ?? DateTime.UtcNow;

        var notifications = await _db.Notifications
            .Where(n => n.CreatedAt >= from && n.CreatedAt <= to)
            .ToListAsync();

        var stats = new
        {
            Period = new { From = from, To = to },
            Summary = new
            {
                TotalSent = notifications.Count,
                ReadCount = notifications.Count(n => n.IsRead),
                UnreadCount = notifications.Count(n => !n.IsRead),
                ReadRate = notifications.Any() ? (double)notifications.Count(n => n.IsRead) / notifications.Count * 100 : 0
            },
            ByType = notifications
                .GroupBy(n => n.Type)
                .Select(g => new
                {
                    Type = g.Key.ToString(),
                    Count = g.Count(),
                    ReadCount = g.Count(n => n.IsRead)
                })
                .ToList(),
            ByDay = notifications
                .GroupBy(n => n.CreatedAt.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Count = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToList()
        };

        return Ok(stats);
    }

    /// <summary>
    /// Preview notification recipients
    /// </summary>
    [HttpPost("preview-recipients")]
    public async Task<ActionResult> PreviewRecipients([FromBody] PreviewRecipientsDto dto)
    {
        var query = _db.Users.AsQueryable();

        if (dto.TargetGroup.HasValue)
        {
            query = dto.TargetGroup.Value switch
            {
                NotificationTargetGroup.AllUsers => query,
                NotificationTargetGroup.AllCustomers => query.Where(u => u.Role == UserRole.Customer),
                NotificationTargetGroup.AllSellers => query.Where(u => u.Role == UserRole.Seller),
                NotificationTargetGroup.ActiveUsers => query.Where(u => (!u.LockoutEndTime.HasValue || u.LockoutEndTime <= DateTime.UtcNow) && u.LastLoginAt >= DateTime.UtcNow.AddDays(-30)),
                _ => query
            };
        }

        if (dto.Role.HasValue)
            query = query.Where(u => u.Role == dto.Role.Value);

        if (dto.IsActive.HasValue)
            query = query.Where(u => dto.IsActive.Value ? (!u.LockoutEndTime.HasValue || u.LockoutEndTime <= DateTime.UtcNow) : (u.LockoutEndTime.HasValue && u.LockoutEndTime > DateTime.UtcNow));

        var count = await query.CountAsync();
        var sampleRecipients = await query
            .Take(10)
            .Select(u => new { u.Id, u.Name, u.Email, u.Role })
            .ToListAsync();

        return Ok(new
        {
            TotalRecipients = count,
            SampleRecipients = sampleRecipients
        });
    }
}

public class SendNotificationDto
{
    public int UserId { get; set; }

    [Required]
    [StringLength(200)]
    public required string Title { get; set; }

    [Required]
    [StringLength(1000)]
    public required string Message { get; set; }

    public NotificationType Type { get; set; } = NotificationType.System;

    [StringLength(500)]
    public string? ActionUrl { get; set; }
}

public class SendBulkNotificationDto
{
    public List<int>? UserIds { get; set; }

    public NotificationTargetGroup? TargetGroup { get; set; }

    [Required]
    [StringLength(200)]
    public required string Title { get; set; }

    [Required]
    [StringLength(1000)]
    public required string Message { get; set; }

    public NotificationType Type { get; set; } = NotificationType.System;

    [StringLength(500)]
    public string? ActionUrl { get; set; }
}

public class SendFilteredNotificationDto
{
    public UserRole? Role { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsEmailVerified { get; set; }
    public DateTime? RegisteredAfter { get; set; }
    public DateTime? RegisteredBefore { get; set; }
    public DateTime? LastLoginAfter { get; set; }
    public bool? HasOrders { get; set; }

    [Required]
    [StringLength(200)]
    public required string Title { get; set; }

    [Required]
    [StringLength(1000)]
    public required string Message { get; set; }

    public NotificationType Type { get; set; } = NotificationType.System;

    [StringLength(500)]
    public string? ActionUrl { get; set; }
}

public class DeleteBulkNotificationsDto
{
    public required List<int> NotificationIds { get; set; }
}

public class PreviewRecipientsDto
{
    public NotificationTargetGroup? TargetGroup { get; set; }
    public UserRole? Role { get; set; }
    public bool? IsActive { get; set; }
}

public enum NotificationTargetGroup
{
    AllUsers,
    AllCustomers,
    AllSellers,
    ActiveUsers,
    VerifiedSellers
}
