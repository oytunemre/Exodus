using FarmazonDemo.Data;
using FarmazonDemo.Models.Entities;
using FarmazonDemo.Services.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace FarmazonDemo.Controllers.Admin;

[Route("api/admin/affiliates")]
[ApiController]
[Authorize(Policy = "AdminOnly")]
public class AdminAffiliateController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public AdminAffiliateController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult> GetAffiliates(
        [FromQuery] AffiliateStatus? status = null,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = "createdAt",
        [FromQuery] bool sortDesc = true,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _db.Set<Affiliate>()
            .Include(a => a.User)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(a =>
                a.ReferralCode.Contains(search) ||
                a.User.Email.Contains(search) ||
                a.User.Name.Contains(search));

        query = sortBy?.ToLower() switch
        {
            "earnings" => sortDesc ? query.OrderByDescending(a => a.TotalEarnings) : query.OrderBy(a => a.TotalEarnings),
            "referrals" => sortDesc ? query.OrderByDescending(a => a.TotalReferrals) : query.OrderBy(a => a.TotalReferrals),
            "status" => sortDesc ? query.OrderByDescending(a => a.Status) : query.OrderBy(a => a.Status),
            _ => sortDesc ? query.OrderByDescending(a => a.CreatedAt) : query.OrderBy(a => a.CreatedAt)
        };

        var totalCount = await query.CountAsync();

        var affiliates = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new
            {
                a.Id,
                a.UserId,
                UserEmail = a.User.Email,
                UserName = a.User.Name,
                a.ReferralCode,
                a.CommissionRate,
                a.MinPayoutAmount,
                a.TotalReferrals,
                a.SuccessfulReferrals,
                a.TotalEarnings,
                a.PendingEarnings,
                a.PaidEarnings,
                a.Status,
                StatusName = a.Status.ToString(),
                a.ApprovedAt,
                a.CreatedAt
            })
            .ToListAsync();

        return Ok(new
        {
            Items = affiliates,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetAffiliate(int id)
    {
        var affiliate = await _db.Set<Affiliate>()
            .Include(a => a.User)
            .Include(a => a.Referrals)
                .ThenInclude(r => r.ReferredUser)
            .Include(a => a.Payouts)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (affiliate == null) throw new NotFoundException("Affiliate not found");

        return Ok(new
        {
            affiliate.Id,
            affiliate.UserId,
            User = new
            {
                affiliate.User.Id,
                affiliate.User.Email,
                Name = affiliate.User.Name,
                affiliate.User.Phone
            },
            affiliate.ReferralCode,
            affiliate.CommissionRate,
            affiliate.MinPayoutAmount,
            affiliate.TotalReferrals,
            affiliate.SuccessfulReferrals,
            affiliate.TotalEarnings,
            affiliate.PendingEarnings,
            affiliate.PaidEarnings,
            affiliate.Status,
            StatusName = affiliate.Status.ToString(),
            affiliate.ApprovedAt,
            affiliate.ApprovedByUserId,
            BankInfo = new
            {
                affiliate.BankName,
                affiliate.IBAN,
                affiliate.AccountHolderName
            },
            RecentReferrals = affiliate.Referrals
                .OrderByDescending(r => r.CreatedAt)
                .Take(10)
                .Select(r => new
                {
                    r.Id,
                    r.ReferredUserId,
                    ReferredUserEmail = r.ReferredUser.Email,
                    r.OrderId,
                    r.OrderAmount,
                    r.CommissionAmount,
                    r.Status,
                    StatusName = r.Status.ToString(),
                    r.CreatedAt
                }),
            RecentPayouts = affiliate.Payouts
                .OrderByDescending(p => p.CreatedAt)
                .Take(10)
                .Select(p => new
                {
                    p.Id,
                    p.PayoutNumber,
                    p.Amount,
                    p.Currency,
                    p.Status,
                    StatusName = p.Status.ToString(),
                    p.PaidAt,
                    p.CreatedAt
                }),
            affiliate.CreatedAt,
            affiliate.UpdatedAt
        });
    }

    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult> UpdateStatus(int id, [FromBody] UpdateAffiliateStatusDto dto)
    {
        var affiliate = await _db.Set<Affiliate>().FindAsync(id);
        if (affiliate == null) throw new NotFoundException("Affiliate not found");

        affiliate.Status = dto.Status;

        if (dto.Status == AffiliateStatus.Approved && !affiliate.ApprovedAt.HasValue)
        {
            affiliate.ApprovedAt = DateTime.UtcNow;
            affiliate.ApprovedByUserId = 1; // Should get from context
        }

        await _db.SaveChangesAsync();

        return Ok(new { Message = "Affiliate status updated", Status = affiliate.Status });
    }

    [HttpPatch("{id:int}/commission")]
    public async Task<ActionResult> UpdateCommission(int id, [FromBody] UpdateAffiliateCommissionDto dto)
    {
        var affiliate = await _db.Set<Affiliate>().FindAsync(id);
        if (affiliate == null) throw new NotFoundException("Affiliate not found");

        if (dto.CommissionRate.HasValue)
            affiliate.CommissionRate = dto.CommissionRate.Value;

        if (dto.MinPayoutAmount.HasValue)
            affiliate.MinPayoutAmount = dto.MinPayoutAmount.Value;

        await _db.SaveChangesAsync();

        return Ok(new
        {
            Message = "Commission settings updated",
            CommissionRate = affiliate.CommissionRate,
            MinPayoutAmount = affiliate.MinPayoutAmount
        });
    }

    [HttpPatch("{id:int}/bank-info")]
    public async Task<ActionResult> UpdateBankInfo(int id, [FromBody] UpdateAffiliateBankInfoDto dto)
    {
        var affiliate = await _db.Set<Affiliate>().FindAsync(id);
        if (affiliate == null) throw new NotFoundException("Affiliate not found");

        if (dto.BankName != null) affiliate.BankName = dto.BankName;
        if (dto.IBAN != null) affiliate.IBAN = dto.IBAN;
        if (dto.AccountHolderName != null) affiliate.AccountHolderName = dto.AccountHolderName;

        await _db.SaveChangesAsync();

        return Ok(new { Message = "Bank information updated" });
    }

    // Referrals
    [HttpGet("{id:int}/referrals")]
    public async Task<ActionResult> GetReferrals(
        int id,
        [FromQuery] AffiliateReferralStatus? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _db.Set<AffiliateReferral>()
            .Include(r => r.ReferredUser)
            .Include(r => r.Order)
            .Where(r => r.AffiliateId == id);

        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        var totalCount = await query.CountAsync();

        var referrals = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new
            {
                r.Id,
                r.ReferredUserId,
                ReferredUserEmail = r.ReferredUser.Email,
                ReferredUserName = r.ReferredUser.Name,
                r.OrderId,
                OrderNumber = r.Order != null ? r.Order.OrderNumber : null,
                r.OrderAmount,
                r.CommissionAmount,
                r.Status,
                StatusName = r.Status.ToString(),
                r.ReferralUrl,
                r.UtmSource,
                r.UtmMedium,
                r.UtmCampaign,
                r.CreatedAt
            })
            .ToListAsync();

        return Ok(new
        {
            Items = referrals,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
    }

    [HttpPatch("referrals/{referralId:int}/status")]
    public async Task<ActionResult> UpdateReferralStatus(int referralId, [FromBody] UpdateReferralStatusDto dto)
    {
        var referral = await _db.Set<AffiliateReferral>()
            .Include(r => r.Affiliate)
            .FirstOrDefaultAsync(r => r.Id == referralId);

        if (referral == null) throw new NotFoundException("Referral not found");

        var oldStatus = referral.Status;
        referral.Status = dto.Status;

        // Update affiliate earnings
        if (dto.Status == AffiliateReferralStatus.Approved && oldStatus != AffiliateReferralStatus.Approved)
        {
            referral.Affiliate.PendingEarnings += referral.CommissionAmount;
        }
        else if (oldStatus == AffiliateReferralStatus.Approved && dto.Status != AffiliateReferralStatus.Approved)
        {
            referral.Affiliate.PendingEarnings -= referral.CommissionAmount;
        }

        await _db.SaveChangesAsync();

        return Ok(new { Message = "Referral status updated", Status = referral.Status });
    }

    // Payouts
    [HttpGet("payouts")]
    public async Task<ActionResult> GetAllPayouts(
        [FromQuery] PayoutStatus? status = null,
        [FromQuery] int? affiliateId = null,
        [FromQuery] string? sortBy = "createdAt",
        [FromQuery] bool sortDesc = true,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _db.Set<AffiliatePayout>()
            .Include(p => p.Affiliate)
                .ThenInclude(a => a.User)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);

        if (affiliateId.HasValue)
            query = query.Where(p => p.AffiliateId == affiliateId.Value);

        query = sortBy?.ToLower() switch
        {
            "amount" => sortDesc ? query.OrderByDescending(p => p.Amount) : query.OrderBy(p => p.Amount),
            "status" => sortDesc ? query.OrderByDescending(p => p.Status) : query.OrderBy(p => p.Status),
            _ => sortDesc ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt)
        };

        var totalCount = await query.CountAsync();

        var payouts = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new
            {
                p.Id,
                p.PayoutNumber,
                p.AffiliateId,
                AffiliateEmail = p.Affiliate.User.Email,
                AffiliateName = p.Affiliate.User.Name,
                p.Amount,
                p.Currency,
                p.Status,
                StatusName = p.Status.ToString(),
                p.TransferReference,
                p.PaidAt,
                p.Notes,
                p.CreatedAt
            })
            .ToListAsync();

        return Ok(new
        {
            Items = payouts,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
    }

    [HttpPost("{id:int}/payouts")]
    public async Task<ActionResult> CreatePayout(int id, [FromBody] CreateAffiliatePayoutDto dto)
    {
        var affiliate = await _db.Set<Affiliate>().FindAsync(id);
        if (affiliate == null) throw new NotFoundException("Affiliate not found");

        if (dto.Amount > affiliate.PendingEarnings)
            throw new ValidationException("Amount exceeds pending earnings");

        var payoutNumber = $"AFF-{DateTime.UtcNow:yyyyMMdd}-{new Random().Next(1000, 9999)}";

        var payout = new AffiliatePayout
        {
            AffiliateId = id,
            PayoutNumber = payoutNumber,
            Amount = dto.Amount,
            Currency = "TRY",
            Status = PayoutStatus.Pending,
            Notes = dto.Notes
        };

        _db.Set<AffiliatePayout>().Add(payout);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            Message = "Payout created",
            PayoutId = payout.Id,
            PayoutNumber = payout.PayoutNumber
        });
    }

    [HttpPatch("payouts/{payoutId:int}/process")]
    public async Task<ActionResult> ProcessPayout(int payoutId, [FromBody] ProcessAffiliatePayoutDto dto)
    {
        var payout = await _db.Set<AffiliatePayout>()
            .Include(p => p.Affiliate)
            .FirstOrDefaultAsync(p => p.Id == payoutId);

        if (payout == null) throw new NotFoundException("Payout not found");

        payout.Status = dto.Status;
        payout.TransferReference = dto.TransferReference;
        payout.Notes = dto.Notes;

        if (dto.Status == PayoutStatus.Paid)
        {
            payout.PaidAt = DateTime.UtcNow;
            payout.PaidByUserId = 1; // Should get from context

            // Update affiliate balances
            payout.Affiliate.PendingEarnings -= payout.Amount;
            payout.Affiliate.PaidEarnings += payout.Amount;
        }

        await _db.SaveChangesAsync();

        return Ok(new { Message = "Payout processed", Status = payout.Status });
    }

    [HttpGet("statistics")]
    public async Task<ActionResult> GetStatistics([FromQuery] int days = 30)
    {
        var fromDate = DateTime.UtcNow.AddDays(-days);

        var affiliates = await _db.Set<Affiliate>().ToListAsync();
        var referrals = await _db.Set<AffiliateReferral>()
            .Where(r => r.CreatedAt >= fromDate)
            .ToListAsync();
        var payouts = await _db.Set<AffiliatePayout>()
            .Where(p => p.CreatedAt >= fromDate)
            .ToListAsync();

        var stats = new
        {
            TotalAffiliates = affiliates.Count,
            ActiveAffiliates = affiliates.Count(a => a.Status == AffiliateStatus.Approved),
            PendingApproval = affiliates.Count(a => a.Status == AffiliateStatus.Pending),

            TotalEarnings = affiliates.Sum(a => a.TotalEarnings),
            TotalPaidOut = affiliates.Sum(a => a.PaidEarnings),
            TotalPending = affiliates.Sum(a => a.PendingEarnings),

            PeriodReferrals = referrals.Count,
            PeriodCommissions = referrals.Sum(r => r.CommissionAmount),
            PeriodPayouts = payouts.Where(p => p.Status == PayoutStatus.Paid).Sum(p => p.Amount),

            ByStatus = affiliates
                .GroupBy(a => a.Status)
                .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
                .ToList(),

            TopAffiliates = affiliates
                .OrderByDescending(a => a.TotalEarnings)
                .Take(10)
                .Select(a => new
                {
                    a.Id,
                    a.ReferralCode,
                    a.TotalReferrals,
                    a.SuccessfulReferrals,
                    a.TotalEarnings
                })
                .ToList()
        };

        return Ok(stats);
    }
}

public class UpdateAffiliateStatusDto { public AffiliateStatus Status { get; set; } }
public class UpdateAffiliateCommissionDto { public decimal? CommissionRate { get; set; } public decimal? MinPayoutAmount { get; set; } }
public class UpdateAffiliateBankInfoDto { public string? BankName { get; set; } public string? IBAN { get; set; } public string? AccountHolderName { get; set; } }
public class UpdateReferralStatusDto { public AffiliateReferralStatus Status { get; set; } }
public class CreateAffiliatePayoutDto { [Required] public decimal Amount { get; set; } public string? Notes { get; set; } }
public class ProcessAffiliatePayoutDto { public PayoutStatus Status { get; set; } public string? TransferReference { get; set; } public string? Notes { get; set; } }
