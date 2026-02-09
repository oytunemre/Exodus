using Exodus.Data;
using Exodus.Models.Entities;
using Exodus.Models.Enums;
using Exodus.Services.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Exodus.Controllers.Admin;

[Route("api/admin/sellers")]
[ApiController]
[Authorize(Policy = "AdminOnly")]
public class AdminSellerController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public AdminSellerController(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Get all sellers with filtering
    /// </summary>
    [HttpGet]
    public async Task<ActionResult> GetSellers(
        [FromQuery] SellerVerificationStatus? status = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = "createdAt",
        [FromQuery] bool sortDesc = true,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _db.Users
            .Where(u => u.Role == UserRole.Seller)
            .AsQueryable();

        // Search
        if (!string.IsNullOrEmpty(search))
            query = query.Where(u =>
                u.Name.Contains(search) ||
                u.Email.Contains(search) ||
                (u.Phone != null && u.Phone.Contains(search)));

        if (isActive.HasValue)
            query = query.Where(u => isActive.Value ? (!u.LockoutEndTime.HasValue || u.LockoutEndTime <= DateTime.UtcNow) : (u.LockoutEndTime.HasValue && u.LockoutEndTime > DateTime.UtcNow));

        // Sorting
        query = sortBy?.ToLower() switch
        {
            "name" => sortDesc ? query.OrderByDescending(u => u.Name) : query.OrderBy(u => u.Name),
            "email" => sortDesc ? query.OrderByDescending(u => u.Email) : query.OrderBy(u => u.Email),
            _ => sortDesc ? query.OrderByDescending(u => u.CreatedAt) : query.OrderBy(u => u.CreatedAt)
        };

        var totalCount = await query.CountAsync();

        var sellers = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new
            {
                u.Id,
                u.Name,
                u.Email,
                u.Phone,
                IsActive = !u.LockoutEndTime.HasValue || u.LockoutEndTime <= DateTime.UtcNow,
                u.EmailVerified,
                u.CreatedAt,
                u.LastLoginAt,
                Profile = _db.SellerProfiles
                    .Where(sp => sp.UserId == u.Id)
                    .Select(sp => new
                    {
                        sp.BusinessName,
                        sp.VerificationStatus,
                        sp.CustomCommissionRate,
                        sp.Rating,
                        sp.TotalSales,
                        sp.TotalRevenue,
                        sp.WarningCount,
                        IsSuspended = sp.SuspendedUntil.HasValue && sp.SuspendedUntil > DateTime.UtcNow
                    })
                    .FirstOrDefault(),
                ListingCount = _db.Listings.Count(l => l.SellerId == u.Id && !l.IsDeleted),
                ActiveListingCount = _db.Listings.Count(l => l.SellerId == u.Id && l.IsActive && !l.IsDeleted),
                OrderCount = _db.SellerOrders.Count(so => so.SellerId == u.Id && !so.IsDeleted)
            })
            .ToListAsync();

        // Filter by verification status (post-query filter since it's in related entity)
        if (status.HasValue)
        {
            sellers = sellers.Where(s => s.Profile?.VerificationStatus == status.Value).ToList();
            totalCount = sellers.Count;
        }

        return Ok(new
        {
            Items = sellers,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        });
    }

    /// <summary>
    /// Get seller details
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetSeller(int id)
    {
        var seller = await _db.Users
            .Where(u => u.Id == id && u.Role == UserRole.Seller)
            .Select(u => new
            {
                u.Id,
                u.Name,
                u.Email,
                u.Phone,
                u.Username,
                IsActive = !u.LockoutEndTime.HasValue || u.LockoutEndTime <= DateTime.UtcNow,
                u.EmailVerified,
                u.LastLoginAt,
                u.CreatedAt,
                u.UpdatedAt
            })
            .FirstOrDefaultAsync();

        if (seller == null)
            throw new NotFoundException("Seller not found");

        var profile = await _db.SellerProfiles
            .Where(sp => sp.UserId == id)
            .FirstOrDefaultAsync();

        // Get default commission rate if no custom rate
        var defaultCommission = await _db.SiteSettings
            .Where(s => s.Key == "Commission.DefaultRate")
            .Select(s => s.Value)
            .FirstOrDefaultAsync();
        var defaultRate = decimal.TryParse(defaultCommission, out var rate) ? rate : 10m;

        // Get statistics
        var stats = new
        {
            TotalListings = await _db.Listings.CountAsync(l => l.SellerId == id),
            ActiveListings = await _db.Listings.CountAsync(l => l.SellerId == id && l.IsActive),
            TotalOrders = await _db.SellerOrders.CountAsync(so => so.SellerId == id),
            CompletedOrders = await _db.SellerOrders.CountAsync(so => so.SellerId == id && so.Status == SellerOrderStatus.Delivered),
            TotalRevenue = await _db.SellerOrders
                .Where(so => so.SellerId == id && so.Status == SellerOrderStatus.Delivered)
                .SumAsync(so => (decimal?)so.SubTotal) ?? 0,
            TotalProducts = await _db.Listings
                .Where(l => l.SellerId == id)
                .Select(l => l.ProductId)
                .Distinct()
                .CountAsync(),
            AverageOrderValue = await _db.SellerOrders
                .Where(so => so.SellerId == id)
                .AverageAsync(so => (decimal?)so.SubTotal) ?? 0
        };

        // Recent orders
        var recentOrders = await _db.SellerOrders
            .Where(so => so.SellerId == id)
            .OrderByDescending(so => so.CreatedAt)
            .Take(5)
            .Select(so => new
            {
                so.Id,
                so.OrderId,
                OrderNumber = so.Order.OrderNumber,
                so.SubTotal,
                so.Status,
                so.CreatedAt
            })
            .ToListAsync();

        return Ok(new
        {
            Seller = seller,
            Profile = profile != null ? new
            {
                profile.BusinessName,
                profile.TaxNumber,
                profile.BusinessAddress,
                profile.BusinessPhone,
                profile.VerificationStatus,
                profile.VerifiedAt,
                profile.RejectionReason,
                profile.TaxDocumentUrl,
                profile.IdentityDocumentUrl,
                profile.SignatureCircularUrl,
                profile.CustomCommissionRate,
                EffectiveCommissionRate = profile.CustomCommissionRate ?? defaultRate,
                profile.BankName,
                profile.IBAN,
                profile.AccountHolderName,
                profile.Rating,
                profile.RatingCount,
                profile.TotalSales,
                profile.TotalRevenue,
                profile.WarningCount,
                profile.SuspendedUntil,
                profile.SuspensionReason
            } : null,
            DefaultCommissionRate = defaultRate,
            Stats = stats,
            RecentOrders = recentOrders
        });
    }

    /// <summary>
    /// Get pending seller applications
    /// </summary>
    [HttpGet("applications")]
    public async Task<ActionResult> GetApplications(
        [FromQuery] SellerVerificationStatus? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _db.SellerProfiles
            .Include(sp => sp.User)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(sp => sp.VerificationStatus == status.Value);
        else
            query = query.Where(sp =>
                sp.VerificationStatus == SellerVerificationStatus.Pending ||
                sp.VerificationStatus == SellerVerificationStatus.UnderReview);

        var totalCount = await query.CountAsync();

        var applications = await query
            .OrderBy(sp => sp.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(sp => new
            {
                sp.Id,
                sp.UserId,
                UserName = sp.User.Name,
                UserEmail = sp.User.Email,
                sp.BusinessName,
                sp.TaxNumber,
                sp.VerificationStatus,
                HasDocuments = sp.TaxDocumentUrl != null || sp.IdentityDocumentUrl != null,
                sp.CreatedAt
            })
            .ToListAsync();

        return Ok(new
        {
            Items = applications,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
    }

    /// <summary>
    /// Review seller application (approve/reject)
    /// </summary>
    [HttpPost("{id:int}/review")]
    public async Task<ActionResult> ReviewApplication(int id, [FromBody] ReviewApplicationDto dto)
    {
        var adminId = GetCurrentUserId();

        var profile = await _db.SellerProfiles.FirstOrDefaultAsync(sp => sp.UserId == id);
        if (profile == null)
        {
            // Create profile if doesn't exist
            profile = new SellerProfile { UserId = id };
            _db.SellerProfiles.Add(profile);
        }

        var user = await _db.Users.FindAsync(id);
        if (user == null || user.Role != UserRole.Seller)
            throw new NotFoundException("Seller not found");

        if (dto.Approve)
        {
            profile.VerificationStatus = SellerVerificationStatus.Approved;
            profile.VerifiedAt = DateTime.UtcNow;
            profile.VerifiedByAdminId = adminId;
            profile.RejectionReason = null;
            user.LockoutEndTime = null; // Activate user
        }
        else
        {
            if (string.IsNullOrEmpty(dto.RejectionReason))
                throw new BadRequestException("Rejection reason is required");

            profile.VerificationStatus = SellerVerificationStatus.Rejected;
            profile.RejectionReason = dto.RejectionReason;
        }

        await _db.SaveChangesAsync();

        return Ok(new
        {
            Message = dto.Approve ? "Seller application approved" : "Seller application rejected",
            SellerId = id,
            Status = profile.VerificationStatus
        });
    }

    /// <summary>
    /// Update seller commission rate
    /// </summary>
    [HttpPatch("{id:int}/commission")]
    public async Task<ActionResult> UpdateCommission(int id, [FromBody] UpdateCommissionDto dto)
    {
        var profile = await _db.SellerProfiles.FirstOrDefaultAsync(sp => sp.UserId == id);
        if (profile == null)
        {
            var userExists = await _db.Users.AnyAsync(u => u.Id == id && u.Role == UserRole.Seller);
            if (!userExists)
                throw new NotFoundException("Seller not found");

            profile = new SellerProfile { UserId = id };
            _db.SellerProfiles.Add(profile);
        }

        if (dto.CommissionRate.HasValue && (dto.CommissionRate < 0 || dto.CommissionRate > 100))
            throw new BadRequestException("Commission rate must be between 0 and 100");

        profile.CustomCommissionRate = dto.CommissionRate;
        await _db.SaveChangesAsync();

        return Ok(new
        {
            Message = dto.CommissionRate.HasValue
                ? $"Custom commission rate set to {dto.CommissionRate}%"
                : "Commission rate reset to default",
            SellerId = id,
            CustomCommissionRate = dto.CommissionRate
        });
    }

    /// <summary>
    /// Suspend seller
    /// </summary>
    [HttpPost("{id:int}/suspend")]
    public async Task<ActionResult> SuspendSeller(int id, [FromBody] SuspendSellerDto dto)
    {
        var profile = await _db.SellerProfiles.FirstOrDefaultAsync(sp => sp.UserId == id);
        if (profile == null)
        {
            var userExists = await _db.Users.AnyAsync(u => u.Id == id && u.Role == UserRole.Seller);
            if (!userExists)
                throw new NotFoundException("Seller not found");

            profile = new SellerProfile { UserId = id };
            _db.SellerProfiles.Add(profile);
        }

        profile.VerificationStatus = SellerVerificationStatus.Suspended;
        profile.SuspendedUntil = dto.IsPermanent ? null : DateTime.UtcNow.AddDays(dto.DurationDays ?? 30);
        profile.SuspensionReason = dto.Reason;

        // Deactivate all listings
        var listings = await _db.Listings.Where(l => l.SellerId == id && l.IsActive).ToListAsync();
        foreach (var listing in listings)
        {
            listing.IsActive = false;
        }

        // Deactivate user
        var user = await _db.Users.FindAsync(id);
        if (user != null)
            user.LockoutEndTime = DateTime.MaxValue; // Deactivate user

        await _db.SaveChangesAsync();

        return Ok(new
        {
            Message = dto.IsPermanent ? "Seller permanently suspended" : $"Seller suspended for {dto.DurationDays ?? 30} days",
            SellerId = id,
            SuspendedUntil = profile.SuspendedUntil,
            ListingsDeactivated = listings.Count
        });
    }

    /// <summary>
    /// Unsuspend seller
    /// </summary>
    [HttpPost("{id:int}/unsuspend")]
    public async Task<ActionResult> UnsuspendSeller(int id)
    {
        var profile = await _db.SellerProfiles.FirstOrDefaultAsync(sp => sp.UserId == id);
        if (profile == null)
            throw new NotFoundException("Seller profile not found");

        if (profile.VerificationStatus != SellerVerificationStatus.Suspended)
            throw new BadRequestException("Seller is not suspended");

        profile.VerificationStatus = SellerVerificationStatus.Approved;
        profile.SuspendedUntil = null;
        profile.SuspensionReason = null;

        // Reactivate user
        var user = await _db.Users.FindAsync(id);
        if (user != null)
            user.LockoutEndTime = null; // Activate user

        await _db.SaveChangesAsync();

        return Ok(new { Message = "Seller unsuspended", SellerId = id });
    }

    /// <summary>
    /// Add warning to seller
    /// </summary>
    [HttpPost("{id:int}/warning")]
    public async Task<ActionResult> AddWarning(int id, [FromBody] AddWarningDto dto)
    {
        var profile = await _db.SellerProfiles.FirstOrDefaultAsync(sp => sp.UserId == id);
        if (profile == null)
        {
            var userExists = await _db.Users.AnyAsync(u => u.Id == id && u.Role == UserRole.Seller);
            if (!userExists)
                throw new NotFoundException("Seller not found");

            profile = new SellerProfile { UserId = id };
            _db.SellerProfiles.Add(profile);
        }

        profile.WarningCount++;
        await _db.SaveChangesAsync();

        // TODO: Send notification to seller

        return Ok(new
        {
            Message = "Warning added to seller",
            SellerId = id,
            WarningCount = profile.WarningCount,
            Reason = dto.Reason
        });
    }

    /// <summary>
    /// Get seller payouts/earnings
    /// </summary>
    [HttpGet("{id:int}/earnings")]
    public async Task<ActionResult> GetSellerEarnings(
        int id,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var seller = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && u.Role == UserRole.Seller);
        if (seller == null)
            throw new NotFoundException("Seller not found");

        var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
        var to = toDate ?? DateTime.UtcNow;

        var profile = await _db.SellerProfiles.FirstOrDefaultAsync(sp => sp.UserId == id);

        // Get default commission rate
        var defaultCommission = await _db.SiteSettings
            .Where(s => s.Key == "Commission.DefaultRate")
            .Select(s => s.Value)
            .FirstOrDefaultAsync();
        var commissionRate = profile?.CustomCommissionRate ?? (decimal.TryParse(defaultCommission, out var rate) ? rate : 10m);

        var orders = await _db.SellerOrders
            .Where(so => so.SellerId == id && so.CreatedAt >= from && so.CreatedAt <= to)
            .Include(so => so.Order)
            .ToListAsync();

        var completedOrders = orders.Where(o => o.Status == SellerOrderStatus.Delivered).ToList();

        var grossRevenue = completedOrders.Sum(o => o.SubTotal);
        var totalCommission = grossRevenue * (commissionRate / 100);
        var netEarnings = grossRevenue - totalCommission;

        var dailyEarnings = completedOrders
            .GroupBy(o => o.CreatedAt.Date)
            .Select(g => new
            {
                Date = g.Key,
                GrossRevenue = g.Sum(o => o.SubTotal),
                Commission = g.Sum(o => o.SubTotal) * (commissionRate / 100),
                NetEarnings = g.Sum(o => o.SubTotal) - (g.Sum(o => o.SubTotal) * (commissionRate / 100)),
                OrderCount = g.Count()
            })
            .OrderBy(d => d.Date)
            .ToList();

        return Ok(new
        {
            Period = new { From = from, To = to },
            CommissionRate = commissionRate,
            Summary = new
            {
                TotalOrders = orders.Count,
                CompletedOrders = completedOrders.Count,
                GrossRevenue = grossRevenue,
                TotalCommission = totalCommission,
                NetEarnings = netEarnings
            },
            DailyBreakdown = dailyEarnings
        });
    }

    /// <summary>
    /// Get seller statistics overview
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult> GetStatistics()
    {
        var sellers = await _db.Users.Where(u => u.Role == UserRole.Seller).ToListAsync();
        var profiles = await _db.SellerProfiles.ToListAsync();

        var stats = new
        {
            TotalSellers = sellers.Count,
            ActiveSellers = sellers.Count(s => !s.LockoutEndTime.HasValue || s.LockoutEndTime <= DateTime.UtcNow),
            InactiveSellers = sellers.Count(s => s.LockoutEndTime.HasValue && s.LockoutEndTime > DateTime.UtcNow),

            ByVerificationStatus = profiles
                .GroupBy(p => p.VerificationStatus)
                .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
                .ToList(),

            PendingApplications = profiles.Count(p => p.VerificationStatus == SellerVerificationStatus.Pending),
            SuspendedSellers = profiles.Count(p => p.VerificationStatus == SellerVerificationStatus.Suspended),

            TopSellers = await _db.SellerOrders
                .Where(so => so.Status == SellerOrderStatus.Delivered)
                .GroupBy(so => new { so.SellerId, so.Seller.Name })
                .Select(g => new
                {
                    SellerId = g.Key.SellerId,
                    SellerName = g.Key.Name,
                    TotalRevenue = g.Sum(so => so.SubTotal),
                    OrderCount = g.Count()
                })
                .OrderByDescending(x => x.TotalRevenue)
                .Take(10)
                .ToListAsync(),

            NewSellersThisMonth = sellers.Count(s => s.CreatedAt >= DateTime.UtcNow.AddDays(-30))
        };

        return Ok(stats);
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedException("Invalid user token");
        return userId;
    }
}

public class ReviewApplicationDto
{
    public bool Approve { get; set; }

    [StringLength(500)]
    public string? RejectionReason { get; set; }
}

public class UpdateCommissionDto
{
    [Range(0, 100)]
    public decimal? CommissionRate { get; set; }
}

public class SuspendSellerDto
{
    [Required]
    [StringLength(500)]
    public required string Reason { get; set; }

    public int? DurationDays { get; set; } = 30;
    public bool IsPermanent { get; set; } = false;
}

public class AddWarningDto
{
    [Required]
    [StringLength(500)]
    public required string Reason { get; set; }
}
