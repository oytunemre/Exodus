using FarmazonDemo.Data;
using FarmazonDemo.Models.Entities;
using FarmazonDemo.Models.Enums;
using FarmazonDemo.Services.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FarmazonDemo.Controllers.Admin;

[Route("api/admin/payments")]
[ApiController]
[Authorize(Policy = "AdminOnly")]
public class AdminPaymentController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public AdminPaymentController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult> GetPayments(
        [FromQuery] PaymentStatus? status = null,
        [FromQuery] int? orderId = null,
        [FromQuery] int? userId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _db.PaymentIntents.Include(p => p.Order).ThenInclude(o => o.Buyer).AsQueryable();

        if (status.HasValue) query = query.Where(p => p.Status == status.Value);
        if (orderId.HasValue) query = query.Where(p => p.OrderId == orderId.Value);
        if (userId.HasValue) query = query.Where(p => p.Order.BuyerId == userId.Value);
        if (fromDate.HasValue) query = query.Where(p => p.CreatedAt >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(p => p.CreatedAt <= toDate.Value);

        var totalCount = await query.CountAsync();
        var payments = await query.OrderByDescending(p => p.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(p => new {
                p.Id, p.OrderId, OrderNumber = p.Order.OrderNumber, p.Amount, p.Currency, p.Status,
                p.Method, p.ExternalReference, p.CreatedAt,
                Buyer = new { p.Order.Buyer.Id, p.Order.Buyer.Name, p.Order.Buyer.Email }
            }).ToListAsync();

        return Ok(new { Items = payments, TotalCount = totalCount, Page = page, PageSize = pageSize });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetPayment(int id)
    {
        var payment = await _db.PaymentIntents.Include(p => p.Order).ThenInclude(o => o.Buyer)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (payment == null) throw new NotFoundException("Payment not found");

        var events = await _db.PaymentEvents.Where(e => e.PaymentIntentId == id).OrderByDescending(e => e.CreatedAt).ToListAsync();

        return Ok(new { Payment = payment, Events = events });
    }

    [HttpGet("events")]
    public async Task<ActionResult> GetPaymentEvents(
        [FromQuery] int? paymentId = null,
        [FromQuery] string? eventType = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = _db.PaymentEvents.AsQueryable();
        if (paymentId.HasValue) query = query.Where(e => e.PaymentIntentId == paymentId.Value);
        if (!string.IsNullOrEmpty(eventType)) query = query.Where(e => e.EventType.Contains(eventType));

        var totalCount = await query.CountAsync();
        var events = await query.OrderByDescending(e => e.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return Ok(new { Items = events, TotalCount = totalCount, Page = page, PageSize = pageSize });
    }

    [HttpGet("failed")]
    public async Task<ActionResult> GetFailedPayments([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var query = _db.PaymentIntents.Include(p => p.Order).ThenInclude(o => o.Buyer)
            .Where(p => p.Status == PaymentStatus.Failed || p.Status == PaymentStatus.Cancelled);

        var totalCount = await query.CountAsync();
        var payments = await query.OrderByDescending(p => p.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(p => new {
                p.Id, p.OrderId, OrderNumber = p.Order.OrderNumber, p.Amount, p.Status, p.FailureReason, p.CreatedAt,
                Buyer = new { p.Order.Buyer.Id, p.Order.Buyer.Name, p.Order.Buyer.Email }
            }).ToListAsync();

        return Ok(new { Items = payments, TotalCount = totalCount, Page = page, PageSize = pageSize });
    }

    [HttpGet("statistics")]
    public async Task<ActionResult> GetStatistics([FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null)
    {
        var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
        var to = toDate ?? DateTime.UtcNow;

        var payments = await _db.PaymentIntents.Where(p => p.CreatedAt >= from && p.CreatedAt <= to).ToListAsync();

        return Ok(new {
            Period = new { From = from, To = to },
            Total = payments.Count,
            Successful = payments.Count(p => p.Status == PaymentStatus.Captured),
            Failed = payments.Count(p => p.Status == PaymentStatus.Failed),
            Pending = payments.Count(p => p.Status == PaymentStatus.Pending),
            TotalAmount = payments.Where(p => p.Status == PaymentStatus.Captured).Sum(p => p.Amount),
            FailedAmount = payments.Where(p => p.Status == PaymentStatus.Failed).Sum(p => p.Amount),
            SuccessRate = payments.Any() ? (double)payments.Count(p => p.Status == PaymentStatus.Captured) / payments.Count * 100 : 0
        });
    }
}
