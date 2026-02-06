using Exodus.Data;
using Exodus.Models.Entities;
using Exodus.Services.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Exodus.Controllers;

[Route("api/support")]
[ApiController]
[Authorize]
public class SupportController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public SupportController(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Get my tickets
    /// </summary>
    [HttpGet("tickets")]
    public async Task<ActionResult> GetMyTickets(
        [FromQuery] TicketStatus? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var userId = GetCurrentUserId();

        var query = _db.SupportTickets
            .Where(t => t.UserId == userId);

        if (status.HasValue)
            query = query.Where(t => t.Status == status.Value);

        var totalCount = await query.CountAsync();

        var tickets = await query
            .OrderByDescending(t => t.CreatedAt)
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
                t.OrderId,
                OrderNumber = t.Order != null ? t.Order.OrderNumber : null,
                LastMessageAt = t.Messages.OrderByDescending(m => m.CreatedAt).Select(m => m.CreatedAt).FirstOrDefault(),
                UnreadCount = t.Messages.Count(m => !m.IsInternal && m.SenderId != userId && m.CreatedAt > t.UpdatedAt),
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

    /// <summary>
    /// Get ticket details with messages
    /// </summary>
    [HttpGet("tickets/{id:int}")]
    public async Task<ActionResult> GetTicket(int id)
    {
        var userId = GetCurrentUserId();

        var ticket = await _db.SupportTickets
            .Where(t => t.Id == id && t.UserId == userId)
            .Select(t => new
            {
                t.Id,
                t.TicketNumber,
                t.Subject,
                t.Category,
                t.Priority,
                t.Status,
                t.OrderId,
                Order = t.Order != null ? new
                {
                    t.Order.Id,
                    t.Order.OrderNumber,
                    t.Order.TotalAmount,
                    t.Order.Status
                } : null,
                t.SatisfactionRating,
                t.CreatedAt,
                t.ResolvedAt,
                t.ClosedAt,
                Messages = t.Messages
                    .Where(m => !m.IsInternal) // Customer can't see internal notes
                    .OrderBy(m => m.CreatedAt)
                    .Select(m => new
                    {
                        m.Id,
                        m.Message,
                        m.Attachments,
                        m.IsSystemMessage,
                        IsFromSupport = m.SenderId != userId,
                        SenderName = m.Sender.Name,
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
    /// Create new support ticket
    /// </summary>
    [HttpPost("tickets")]
    public async Task<ActionResult> CreateTicket([FromBody] CreateTicketDto dto)
    {
        var userId = GetCurrentUserId();

        // Validate order belongs to user if provided
        if (dto.OrderId.HasValue)
        {
            var orderExists = await _db.Orders.AnyAsync(o => o.Id == dto.OrderId.Value && o.BuyerId == userId);
            if (!orderExists)
                throw new BadRequestException("Order not found");
        }

        var ticketNumber = await GenerateTicketNumberAsync();

        var ticket = new SupportTicket
        {
            TicketNumber = ticketNumber,
            UserId = userId,
            OrderId = dto.OrderId,
            Subject = dto.Subject,
            Category = dto.Category,
            Priority = dto.Priority ?? TicketPriority.Normal,
            Status = TicketStatus.Open
        };

        _db.SupportTickets.Add(ticket);
        await _db.SaveChangesAsync();

        // Add initial message
        var message = new SupportTicketMessage
        {
            TicketId = ticket.Id,
            SenderId = userId,
            Message = dto.Message,
            Attachments = dto.Attachments
        };

        _db.SupportTicketMessages.Add(message);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetTicket), new { id = ticket.Id }, new
        {
            Message = "Ticket created successfully",
            TicketId = ticket.Id,
            TicketNumber = ticketNumber
        });
    }

    /// <summary>
    /// Reply to a ticket
    /// </summary>
    [HttpPost("tickets/{id:int}/messages")]
    public async Task<ActionResult> ReplyToTicket(int id, [FromBody] ReplyTicketDto dto)
    {
        var userId = GetCurrentUserId();

        var ticket = await _db.SupportTickets.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
        if (ticket == null)
            throw new NotFoundException("Ticket not found");

        if (ticket.Status == TicketStatus.Closed)
            throw new BadRequestException("Cannot reply to a closed ticket");

        var message = new SupportTicketMessage
        {
            TicketId = id,
            SenderId = userId,
            Message = dto.Message,
            Attachments = dto.Attachments
        };

        _db.SupportTicketMessages.Add(message);

        // Update ticket status
        if (ticket.Status == TicketStatus.AwaitingCustomerReply)
            ticket.Status = TicketStatus.AwaitingSupport;

        await _db.SaveChangesAsync();

        return Ok(new { Message = "Reply sent successfully" });
    }

    /// <summary>
    /// Close a resolved ticket
    /// </summary>
    [HttpPost("tickets/{id:int}/close")]
    public async Task<ActionResult> CloseTicket(int id, [FromBody] CloseTicketDto? dto = null)
    {
        var userId = GetCurrentUserId();

        var ticket = await _db.SupportTickets.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
        if (ticket == null)
            throw new NotFoundException("Ticket not found");

        if (ticket.Status == TicketStatus.Closed)
            throw new BadRequestException("Ticket is already closed");

        ticket.Status = TicketStatus.Closed;
        ticket.ClosedAt = DateTime.UtcNow;

        if (dto != null)
        {
            if (dto.SatisfactionRating.HasValue)
            {
                if (dto.SatisfactionRating < 1 || dto.SatisfactionRating > 5)
                    throw new BadRequestException("Rating must be between 1 and 5");
                ticket.SatisfactionRating = dto.SatisfactionRating;
            }
            ticket.SatisfactionComment = dto.SatisfactionComment;
        }

        // Add system message
        _db.SupportTicketMessages.Add(new SupportTicketMessage
        {
            TicketId = id,
            SenderId = userId,
            Message = "Ticket closed by customer",
            IsSystemMessage = true
        });

        await _db.SaveChangesAsync();

        return Ok(new { Message = "Ticket closed successfully" });
    }

    /// <summary>
    /// Reopen a closed ticket
    /// </summary>
    [HttpPost("tickets/{id:int}/reopen")]
    public async Task<ActionResult> ReopenTicket(int id, [FromBody] ReopenTicketDto dto)
    {
        var userId = GetCurrentUserId();

        var ticket = await _db.SupportTickets.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
        if (ticket == null)
            throw new NotFoundException("Ticket not found");

        if (ticket.Status != TicketStatus.Closed && ticket.Status != TicketStatus.Resolved)
            throw new BadRequestException("Ticket is not closed or resolved");

        ticket.Status = TicketStatus.Open;
        ticket.ClosedAt = null;
        ticket.ResolvedAt = null;

        // Add reopen message
        _db.SupportTicketMessages.Add(new SupportTicketMessage
        {
            TicketId = id,
            SenderId = userId,
            Message = dto.Reason,
            IsSystemMessage = false
        });

        await _db.SaveChangesAsync();

        return Ok(new { Message = "Ticket reopened successfully" });
    }

    private async Task<string> GenerateTicketNumberAsync()
    {
        var today = DateTime.UtcNow;
        var prefix = $"TKT-{today:yyyyMMdd}";

        var lastTicket = await _db.SupportTickets
            .Where(t => t.TicketNumber.StartsWith(prefix))
            .OrderByDescending(t => t.TicketNumber)
            .FirstOrDefaultAsync();

        int sequence = 1;
        if (lastTicket != null)
        {
            var lastSequence = lastTicket.TicketNumber.Split('-').Last();
            if (int.TryParse(lastSequence, out var num))
                sequence = num + 1;
        }

        return $"{prefix}-{sequence:D4}";
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedException("Invalid user token");
        return userId;
    }
}

public class CreateTicketDto
{
    [Required]
    [StringLength(200)]
    public required string Subject { get; set; }

    [Required]
    public required string Message { get; set; }

    public TicketCategory Category { get; set; } = TicketCategory.General;

    public TicketPriority? Priority { get; set; }

    public int? OrderId { get; set; }

    [StringLength(2000)]
    public string? Attachments { get; set; }
}

public class ReplyTicketDto
{
    [Required]
    public required string Message { get; set; }

    [StringLength(2000)]
    public string? Attachments { get; set; }
}

public class CloseTicketDto
{
    [Range(1, 5)]
    public int? SatisfactionRating { get; set; }

    [StringLength(500)]
    public string? SatisfactionComment { get; set; }
}

public class ReopenTicketDto
{
    [Required]
    public required string Reason { get; set; }
}
