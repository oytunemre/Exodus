using FarmazonDemo.Data;
using FarmazonDemo.Models.Entities;
using FarmazonDemo.Services.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace FarmazonDemo.Controllers.Admin;

[Route("api/admin/support")]
[ApiController]
[Authorize(Policy = "AdminOnly")]
public class AdminSupportController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public AdminSupportController(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Get all tickets with filtering
    /// </summary>
    [HttpGet("tickets")]
    public async Task<ActionResult> GetTickets(
        [FromQuery] TicketStatus? status = null,
        [FromQuery] TicketCategory? category = null,
        [FromQuery] TicketPriority? priority = null,
        [FromQuery] int? assignedToId = null,
        [FromQuery] bool? unassigned = null,
        [FromQuery] int? userId = null,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = "createdAt",
        [FromQuery] bool sortDesc = true,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _db.SupportTickets
            .Include(t => t.User)
            .AsQueryable();

        // Filters
        if (status.HasValue)
            query = query.Where(t => t.Status == status.Value);

        if (category.HasValue)
            query = query.Where(t => t.Category == category.Value);

        if (priority.HasValue)
            query = query.Where(t => t.Priority == priority.Value);

        if (assignedToId.HasValue)
            query = query.Where(t => t.AssignedToId == assignedToId.Value);

        if (unassigned == true)
            query = query.Where(t => t.AssignedToId == null);

        if (userId.HasValue)
            query = query.Where(t => t.UserId == userId.Value);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(t =>
                t.TicketNumber.Contains(search) ||
                t.Subject.Contains(search) ||
                t.User.Name.Contains(search) ||
                t.User.Email.Contains(search));

        // Sorting
        query = sortBy?.ToLower() switch
        {
            "priority" => sortDesc
                ? query.OrderByDescending(t => t.Priority)
                : query.OrderBy(t => t.Priority),
            "status" => sortDesc
                ? query.OrderByDescending(t => t.Status)
                : query.OrderBy(t => t.Status),
            "updatedat" => sortDesc
                ? query.OrderByDescending(t => t.UpdatedAt)
                : query.OrderBy(t => t.UpdatedAt),
            _ => sortDesc
                ? query.OrderByDescending(t => t.CreatedAt)
                : query.OrderBy(t => t.CreatedAt)
        };

        var totalCount = await query.CountAsync();

        var tickets = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new
            {
                t.Id,
                t.TicketNumber,
                t.Subject,
                t.Category,
                t.Priority,
                t.Status,
                User = new { t.User.Id, t.User.Name, t.User.Email },
                t.OrderId,
                OrderNumber = t.Order != null ? t.Order.OrderNumber : null,
                AssignedTo = t.AssignedTo != null ? new { t.AssignedTo.Id, t.AssignedTo.Name } : null,
                MessageCount = t.Messages.Count(m => !m.IsDeleted),
                LastMessageAt = t.Messages.OrderByDescending(m => m.CreatedAt).Select(m => m.CreatedAt).FirstOrDefault(),
                t.FirstResponseAt,
                t.CreatedAt,
                t.UpdatedAt
            })
            .ToListAsync();

        return Ok(new
        {
            Items = tickets,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        });
    }

    /// <summary>
    /// Get ticket details
    /// </summary>
    [HttpGet("tickets/{id:int}")]
    public async Task<ActionResult> GetTicket(int id)
    {
        var ticket = await _db.SupportTickets
            .Include(t => t.User)
            .Include(t => t.AssignedTo)
            .Include(t => t.Order)
            .Where(t => t.Id == id)
            .Select(t => new
            {
                t.Id,
                t.TicketNumber,
                t.Subject,
                t.Category,
                t.Priority,
                t.Status,
                User = new
                {
                    t.User.Id,
                    t.User.Name,
                    t.User.Email,
                    t.User.Phone,
                    t.User.Role,
                    TotalOrders = _db.Orders.Count(o => o.BuyerId == t.UserId),
                    TotalTickets = _db.SupportTickets.Count(st => st.UserId == t.UserId)
                },
                Order = t.Order != null ? new
                {
                    t.Order.Id,
                    t.Order.OrderNumber,
                    t.Order.TotalAmount,
                    t.Order.Status,
                    t.Order.CreatedAt
                } : null,
                AssignedTo = t.AssignedTo != null ? new { t.AssignedTo.Id, t.AssignedTo.Name, t.AssignedTo.Email } : null,
                t.FirstResponseAt,
                t.ResolvedAt,
                t.ClosedAt,
                t.SatisfactionRating,
                t.SatisfactionComment,
                t.CreatedAt,
                t.UpdatedAt,
                Messages = t.Messages
                    .OrderBy(m => m.CreatedAt)
                    .Select(m => new
                    {
                        m.Id,
                        m.Message,
                        m.Attachments,
                        m.IsInternal,
                        m.IsSystemMessage,
                        Sender = new { m.Sender.Id, m.Sender.Name, m.Sender.Role },
                        m.CreatedAt
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (ticket == null)
            throw new NotFoundException("Ticket not found");

        return Ok(ticket);
    }

    /// <summary>
    /// Reply to ticket (admin)
    /// </summary>
    [HttpPost("tickets/{id:int}/reply")]
    public async Task<ActionResult> ReplyToTicket(int id, [FromBody] AdminReplyTicketDto dto)
    {
        var adminId = GetCurrentUserId();

        var ticket = await _db.SupportTickets.FindAsync(id);
        if (ticket == null)
            throw new NotFoundException("Ticket not found");

        if (ticket.Status == TicketStatus.Closed)
            throw new BadRequestException("Cannot reply to a closed ticket");

        var message = new SupportTicketMessage
        {
            TicketId = id,
            SenderId = adminId,
            Message = dto.Message,
            Attachments = dto.Attachments,
            IsInternal = dto.IsInternal
        };

        _db.SupportTicketMessages.Add(message);

        // Update first response time if this is the first staff response
        if (!ticket.FirstResponseAt.HasValue && !dto.IsInternal)
            ticket.FirstResponseAt = DateTime.UtcNow;

        // Update status if not internal note
        if (!dto.IsInternal)
            ticket.Status = TicketStatus.AwaitingCustomerReply;

        await _db.SaveChangesAsync();

        return Ok(new { Message = dto.IsInternal ? "Internal note added" : "Reply sent successfully" });
    }

    /// <summary>
    /// Update ticket status
    /// </summary>
    [HttpPatch("tickets/{id:int}/status")]
    public async Task<ActionResult> UpdateStatus(int id, [FromBody] UpdateTicketStatusDto dto)
    {
        var adminId = GetCurrentUserId();

        var ticket = await _db.SupportTickets.FindAsync(id);
        if (ticket == null)
            throw new NotFoundException("Ticket not found");

        var oldStatus = ticket.Status;
        ticket.Status = dto.Status;

        // Set timestamps based on status
        if (dto.Status == TicketStatus.Resolved && !ticket.ResolvedAt.HasValue)
            ticket.ResolvedAt = DateTime.UtcNow;
        else if (dto.Status == TicketStatus.Closed && !ticket.ClosedAt.HasValue)
            ticket.ClosedAt = DateTime.UtcNow;

        // Add system message
        var admin = await _db.Users.FindAsync(adminId);
        _db.SupportTicketMessages.Add(new SupportTicketMessage
        {
            TicketId = id,
            SenderId = adminId,
            Message = $"Status changed from {oldStatus} to {dto.Status}" +
                      (string.IsNullOrEmpty(dto.Note) ? "" : $": {dto.Note}"),
            IsSystemMessage = true
        });

        await _db.SaveChangesAsync();

        return Ok(new { Message = "Status updated", OldStatus = oldStatus, NewStatus = dto.Status });
    }

    /// <summary>
    /// Assign ticket to admin
    /// </summary>
    [HttpPatch("tickets/{id:int}/assign")]
    public async Task<ActionResult> AssignTicket(int id, [FromBody] AssignTicketDto dto)
    {
        var adminId = GetCurrentUserId();

        var ticket = await _db.SupportTickets.FindAsync(id);
        if (ticket == null)
            throw new NotFoundException("Ticket not found");

        // Validate assignee exists and is admin
        if (dto.AssignToId.HasValue)
        {
            var assignee = await _db.Users.FindAsync(dto.AssignToId.Value);
            if (assignee == null || assignee.Role != Models.Enums.UserRole.Admin)
                throw new BadRequestException("Invalid assignee");
        }

        var oldAssignee = ticket.AssignedToId;
        ticket.AssignedToId = dto.AssignToId;

        if (ticket.Status == TicketStatus.Open)
            ticket.Status = TicketStatus.InProgress;

        // Add system message
        var assigneeName = dto.AssignToId.HasValue
            ? (await _db.Users.FindAsync(dto.AssignToId.Value))?.Name ?? "Unknown"
            : "Unassigned";

        _db.SupportTicketMessages.Add(new SupportTicketMessage
        {
            TicketId = id,
            SenderId = adminId,
            Message = dto.AssignToId.HasValue
                ? $"Ticket assigned to {assigneeName}"
                : "Ticket unassigned",
            IsSystemMessage = true,
            IsInternal = true
        });

        await _db.SaveChangesAsync();

        return Ok(new { Message = "Ticket assigned", AssignedToId = dto.AssignToId });
    }

    /// <summary>
    /// Update ticket priority
    /// </summary>
    [HttpPatch("tickets/{id:int}/priority")]
    public async Task<ActionResult> UpdatePriority(int id, [FromBody] UpdateTicketPriorityDto dto)
    {
        var adminId = GetCurrentUserId();

        var ticket = await _db.SupportTickets.FindAsync(id);
        if (ticket == null)
            throw new NotFoundException("Ticket not found");

        var oldPriority = ticket.Priority;
        ticket.Priority = dto.Priority;

        // Add internal note
        _db.SupportTicketMessages.Add(new SupportTicketMessage
        {
            TicketId = id,
            SenderId = adminId,
            Message = $"Priority changed from {oldPriority} to {dto.Priority}",
            IsSystemMessage = true,
            IsInternal = true
        });

        await _db.SaveChangesAsync();

        return Ok(new { Message = "Priority updated", OldPriority = oldPriority, NewPriority = dto.Priority });
    }

    /// <summary>
    /// Get support statistics
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult> GetStatistics(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
        var to = toDate ?? DateTime.UtcNow;

        var tickets = await _db.SupportTickets
            .Where(t => t.CreatedAt >= from && t.CreatedAt <= to)
            .ToListAsync();

        var allTickets = await _db.SupportTickets.ToListAsync();

        var stats = new
        {
            // Period stats
            Period = new
            {
                From = from,
                To = to,
                TotalTickets = tickets.Count,
                ResolvedTickets = tickets.Count(t => t.Status == TicketStatus.Resolved || t.Status == TicketStatus.Closed),
                AverageFirstResponseTime = tickets
                    .Where(t => t.FirstResponseAt.HasValue)
                    .Select(t => (t.FirstResponseAt!.Value - t.CreatedAt).TotalHours)
                    .DefaultIfEmpty(0)
                    .Average(),
                AverageResolutionTime = tickets
                    .Where(t => t.ResolvedAt.HasValue)
                    .Select(t => (t.ResolvedAt!.Value - t.CreatedAt).TotalHours)
                    .DefaultIfEmpty(0)
                    .Average(),
                AverageSatisfaction = tickets
                    .Where(t => t.SatisfactionRating.HasValue)
                    .Select(t => t.SatisfactionRating!.Value)
                    .DefaultIfEmpty(0)
                    .Average()
            },

            // Current status
            CurrentStatus = new
            {
                OpenTickets = allTickets.Count(t => t.Status == TicketStatus.Open),
                AwaitingSupport = allTickets.Count(t => t.Status == TicketStatus.AwaitingSupport),
                AwaitingCustomer = allTickets.Count(t => t.Status == TicketStatus.AwaitingCustomerReply),
                InProgress = allTickets.Count(t => t.Status == TicketStatus.InProgress),
                OnHold = allTickets.Count(t => t.Status == TicketStatus.OnHold),
                Unassigned = allTickets.Count(t => t.AssignedToId == null &&
                    t.Status != TicketStatus.Resolved && t.Status != TicketStatus.Closed)
            },

            // By category
            ByCategory = tickets
                .GroupBy(t => t.Category)
                .Select(g => new { Category = g.Key.ToString(), Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList(),

            // By priority
            ByPriority = tickets
                .GroupBy(t => t.Priority)
                .Select(g => new { Priority = g.Key.ToString(), Count = g.Count() })
                .ToList(),

            // Top agents
            TopAgents = await _db.SupportTickets
                .Where(t => t.AssignedToId.HasValue && t.CreatedAt >= from && t.CreatedAt <= to)
                .GroupBy(t => new { t.AssignedToId, t.AssignedTo!.Name })
                .Select(g => new
                {
                    AgentId = g.Key.AssignedToId,
                    AgentName = g.Key.Name,
                    TicketsHandled = g.Count(),
                    Resolved = g.Count(t => t.Status == TicketStatus.Resolved || t.Status == TicketStatus.Closed)
                })
                .OrderByDescending(x => x.Resolved)
                .Take(5)
                .ToListAsync()
        };

        return Ok(stats);
    }

    /// <summary>
    /// Get my assigned tickets
    /// </summary>
    [HttpGet("my-tickets")]
    public async Task<ActionResult> GetMyAssignedTickets(
        [FromQuery] TicketStatus? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var adminId = GetCurrentUserId();

        var query = _db.SupportTickets
            .Where(t => t.AssignedToId == adminId);

        if (status.HasValue)
            query = query.Where(t => t.Status == status.Value);

        var totalCount = await query.CountAsync();

        var tickets = await query
            .OrderByDescending(t => t.Priority)
            .ThenByDescending(t => t.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new
            {
                t.Id,
                t.TicketNumber,
                t.Subject,
                t.Category,
                t.Priority,
                t.Status,
                User = new { t.User.Id, t.User.Name },
                t.OrderId,
                LastMessageAt = t.Messages.OrderByDescending(m => m.CreatedAt).Select(m => m.CreatedAt).FirstOrDefault(),
                t.CreatedAt
            })
            .ToListAsync();

        return Ok(new
        {
            Items = tickets,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedException("Invalid user token");
        return userId;
    }
}

public class AdminReplyTicketDto
{
    [Required]
    public required string Message { get; set; }

    [StringLength(2000)]
    public string? Attachments { get; set; }

    public bool IsInternal { get; set; } = false;
}

public class UpdateTicketStatusDto
{
    public TicketStatus Status { get; set; }
    public string? Note { get; set; }
}

public class AssignTicketDto
{
    public int? AssignToId { get; set; }
}

public class UpdateTicketPriorityDto
{
    public TicketPriority Priority { get; set; }
}
