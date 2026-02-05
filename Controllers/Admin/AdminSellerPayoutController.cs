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

[Route("api/admin/seller-payouts")]
[ApiController]
[Authorize(Policy = "AdminOnly")]
public class AdminSellerPayoutController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public AdminSellerPayoutController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult> GetPayouts(
        [FromQuery] PayoutStatus? status = null,
        [FromQuery] int? sellerId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _db.Set<SellerPayout>().Include(p => p.Seller).AsQueryable();

        if (status.HasValue) query = query.Where(p => p.Status == status.Value);
        if (sellerId.HasValue) query = query.Where(p => p.SellerId == sellerId.Value);
        if (fromDate.HasValue) query = query.Where(p => p.CreatedAt >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(p => p.CreatedAt <= toDate.Value);

        var totalCount = await query.CountAsync();
        var payouts = await query.OrderByDescending(p => p.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(p => new {
                p.Id, p.PayoutNumber, p.SellerId, SellerName = p.Seller.Name, p.GrossAmount, p.CommissionAmount,
                p.NetAmount, p.Status, p.OrderCount, p.PeriodStart, p.PeriodEnd, p.CreatedAt, p.PaidAt
            }).ToListAsync();

        return Ok(new { Items = payouts, TotalCount = totalCount, Page = page, PageSize = pageSize });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetPayout(int id)
    {
        var payout = await _db.Set<SellerPayout>().Include(p => p.Seller).Include(p => p.Items)
            .ThenInclude(i => i.SellerOrder).FirstOrDefaultAsync(p => p.Id == id);
        if (payout == null) throw new NotFoundException("Payout not found");
        return Ok(payout);
    }

    [HttpPost("generate")]
    public async Task<ActionResult> GeneratePayout([FromBody] GeneratePayoutDto dto)
    {
        var seller = await _db.Users.FindAsync(dto.SellerId);
        if (seller == null || seller.Role != UserRole.Seller) throw new NotFoundException("Seller not found");

        var orders = await _db.SellerOrders
            .Where(so => so.SellerId == dto.SellerId && so.Status == SellerOrderStatus.Delivered
                && so.CreatedAt >= dto.PeriodStart && so.CreatedAt <= dto.PeriodEnd)
            .ToListAsync();

        if (!orders.Any()) throw new BadRequestException("No completed orders in this period");

        var profile = await _db.SellerProfiles.FirstOrDefaultAsync(p => p.UserId == dto.SellerId);
        var defaultRate = 10m;
        var commissionRate = profile?.CustomCommissionRate ?? defaultRate;

        var grossAmount = orders.Sum(o => o.SubTotal);
        var commissionAmount = grossAmount * (commissionRate / 100);

        var payoutNumber = $"PAY-{DateTime.UtcNow:yyyyMMdd}-{await _db.Set<SellerPayout>().CountAsync() + 1:D4}";

        var payout = new SellerPayout {
            PayoutNumber = payoutNumber, SellerId = dto.SellerId,
            PeriodStart = dto.PeriodStart, PeriodEnd = dto.PeriodEnd,
            GrossAmount = grossAmount, CommissionAmount = commissionAmount,
            RefundDeductions = 0, OtherDeductions = 0, NetAmount = grossAmount - commissionAmount,
            OrderCount = orders.Count, ItemCount = orders.Count,
            BankName = profile?.BankName, IBAN = profile?.IBAN, AccountHolderName = profile?.AccountHolderName
        };

        _db.Set<SellerPayout>().Add(payout);
        await _db.SaveChangesAsync();

        foreach (var order in orders)
        {
            _db.Set<SellerPayoutItem>().Add(new SellerPayoutItem {
                PayoutId = payout.Id, SellerOrderId = order.Id, OrderAmount = order.SubTotal,
                CommissionAmount = order.SubTotal * (commissionRate / 100),
                NetAmount = order.SubTotal - (order.SubTotal * (commissionRate / 100))
            });
        }
        await _db.SaveChangesAsync();

        return Ok(new { Message = "Payout generated", PayoutId = payout.Id, PayoutNumber = payoutNumber });
    }

    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult> UpdateStatus(int id, [FromBody] UpdatePayoutStatusDto dto)
    {
        var payout = await _db.Set<SellerPayout>().FindAsync(id);
        if (payout == null) throw new NotFoundException("Payout not found");

        payout.Status = dto.Status;
        if (dto.Status == PayoutStatus.Approved) { payout.ApprovedAt = DateTime.UtcNow; payout.ApprovedByUserId = GetCurrentUserId(); }
        if (dto.Status == PayoutStatus.Paid) { payout.PaidAt = DateTime.UtcNow; payout.PaidByUserId = GetCurrentUserId(); }
        if (!string.IsNullOrEmpty(dto.TransferReference)) payout.TransferReference = dto.TransferReference;
        if (!string.IsNullOrEmpty(dto.Notes)) payout.Notes = dto.Notes;

        await _db.SaveChangesAsync();
        return Ok(new { Message = "Payout status updated", Status = dto.Status });
    }

    [HttpGet("statistics")]
    public async Task<ActionResult> GetStatistics()
    {
        var payouts = await _db.Set<SellerPayout>().ToListAsync();
        return Ok(new {
            Total = payouts.Count,
            Pending = payouts.Count(p => p.Status == PayoutStatus.Pending),
            Approved = payouts.Count(p => p.Status == PayoutStatus.Approved),
            Paid = payouts.Count(p => p.Status == PayoutStatus.Paid),
            TotalPaid = payouts.Where(p => p.Status == PayoutStatus.Paid).Sum(p => p.NetAmount),
            TotalPending = payouts.Where(p => p.Status == PayoutStatus.Pending || p.Status == PayoutStatus.Approved).Sum(p => p.NetAmount),
            TotalCommission = payouts.Sum(p => p.CommissionAmount)
        });
    }

    private int GetCurrentUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
}

public class GeneratePayoutDto { public int SellerId { get; set; } public DateTime PeriodStart { get; set; } public DateTime PeriodEnd { get; set; } }
public class UpdatePayoutStatusDto { public PayoutStatus Status { get; set; } public string? TransferReference { get; set; } public string? Notes { get; set; } }
