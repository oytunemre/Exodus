using Exodus.Data;
using Exodus.Models.Dto.Campaign;
using Exodus.Models.Entities;
using Exodus.Services.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Exodus.Controllers.Admin;

[Route("api/admin/campaigns")]
[ApiController]
[Authorize(Policy = "AdminOnly")]
public class AdminCampaignController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public AdminCampaignController(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Get all campaigns with filtering
    /// </summary>
    [HttpGet]
    public async Task<ActionResult> GetCampaigns(
        [FromQuery] CampaignType? type = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int? sellerId = null,
        [FromQuery] bool? hasExpired = null,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = "createdAt",
        [FromQuery] bool sortDesc = true,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _db.Campaigns
            .Include(c => c.Seller)
            .AsQueryable();

        var now = DateTime.UtcNow;

        // Filters
        if (type.HasValue)
            query = query.Where(c => c.Type == type.Value);

        if (isActive.HasValue)
            query = query.Where(c => c.IsActive == isActive.Value);

        if (sellerId.HasValue)
            query = query.Where(c => c.SellerId == sellerId.Value);

        if (hasExpired == true)
            query = query.Where(c => c.EndDate < now);
        else if (hasExpired == false)
            query = query.Where(c => c.EndDate >= now);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(c =>
                c.Name.Contains(search) ||
                (c.CouponCode != null && c.CouponCode.Contains(search)) ||
                (c.Description != null && c.Description.Contains(search)));

        // Sorting
        query = sortBy?.ToLower() switch
        {
            "name" => sortDesc ? query.OrderByDescending(c => c.Name) : query.OrderBy(c => c.Name),
            "startdate" => sortDesc ? query.OrderByDescending(c => c.StartDate) : query.OrderBy(c => c.StartDate),
            "enddate" => sortDesc ? query.OrderByDescending(c => c.EndDate) : query.OrderBy(c => c.EndDate),
            "usage" => sortDesc ? query.OrderByDescending(c => c.CurrentUsageCount) : query.OrderBy(c => c.CurrentUsageCount),
            _ => sortDesc ? query.OrderByDescending(c => c.CreatedAt) : query.OrderBy(c => c.CreatedAt)
        };

        var totalCount = await query.CountAsync();

        var campaigns = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Type,
                c.IsActive,
                c.CouponCode,
                c.RequiresCouponCode,
                c.StartDate,
                c.EndDate,
                IsExpired = c.EndDate < now,
                IsCurrentlyActive = c.IsActive && c.StartDate <= now && c.EndDate >= now,
                c.DiscountPercentage,
                c.DiscountAmount,
                c.MaxDiscountAmount,
                c.MinimumOrderAmount,
                c.CurrentUsageCount,
                c.MaxUsageCount,
                Seller = c.Seller != null ? new { c.Seller.Id, c.Seller.Name } : null,
                c.CreatedAt
            })
            .ToListAsync();

        return Ok(new
        {
            Items = campaigns,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        });
    }

    /// <summary>
    /// Get campaign details
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetCampaign(int id)
    {
        var now = DateTime.UtcNow;

        var campaign = await _db.Campaigns
            .Include(c => c.Seller)
            .Include(c => c.CampaignProducts)
                .ThenInclude(cp => cp.Product)
            .Include(c => c.CampaignCategories)
                .ThenInclude(cc => cc.Category)
            .Where(c => c.Id == id)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Description,
                c.Type,
                c.Scope,
                c.IsActive,
                c.StartDate,
                c.EndDate,
                IsExpired = c.EndDate < now,
                IsCurrentlyActive = c.IsActive && c.StartDate <= now && c.EndDate >= now,

                // Discount info
                c.DiscountPercentage,
                c.DiscountAmount,
                c.MaxDiscountAmount,

                // Requirements
                c.MinimumOrderAmount,
                c.MinimumQuantity,

                // BOGO
                c.BuyQuantity,
                c.GetQuantity,

                // Coupon
                c.CouponCode,
                c.RequiresCouponCode,

                // Usage
                c.CurrentUsageCount,
                c.MaxUsageCount,
                c.MaxUsagePerUser,

                // Other
                c.Priority,
                c.IsStackable,

                Seller = c.Seller != null ? new { c.Seller.Id, c.Seller.Name, c.Seller.Email } : null,

                // Targeted products
                Products = c.CampaignProducts
                    .Where(cp => cp.Product != null)
                    .Select(cp => new
                    {
                        cp.Product!.Id,
                        cp.Product.ProductName
                    }).ToList(),

                // Targeted categories
                Categories = c.CampaignCategories
                    .Where(cc => cc.Category != null)
                    .Select(cc => new
                    {
                        cc.Category!.Id,
                        cc.Category.Name
                    }).ToList(),

                // Stats
                TotalDiscountGiven = _db.CampaignUsages
                    .Where(u => u.CampaignId == c.Id)
                    .Sum(u => (decimal?)u.DiscountApplied) ?? 0,

                c.CreatedAt,
                c.UpdatedAt
            })
            .FirstOrDefaultAsync();

        if (campaign == null)
            throw new NotFoundException("Campaign not found");

        return Ok(campaign);
    }

    /// <summary>
    /// Create new campaign
    /// </summary>
    [HttpPost]
    public async Task<ActionResult> CreateCampaign([FromBody] CreateCampaignDto dto)
    {
        // Validate dates
        if (dto.EndDate <= dto.StartDate)
            throw new BadRequestException("End date must be after start date");

        // Validate coupon code uniqueness
        if (!string.IsNullOrEmpty(dto.CouponCode))
        {
            var codeExists = await _db.Campaigns.AnyAsync(c =>
                c.CouponCode == dto.CouponCode && !c.IsDeleted);
            if (codeExists)
                throw new BadRequestException("Coupon code already exists");
        }

        // Validate seller exists
        if (dto.SellerId.HasValue)
        {
            var sellerExists = await _db.Users.AnyAsync(u => u.Id == dto.SellerId.Value);
            if (!sellerExists)
                throw new BadRequestException("Seller not found");
        }

        var campaign = new Campaign
        {
            Name = dto.Name,
            Description = dto.Description,
            Type = dto.Type,
            SellerId = dto.SellerId ?? 0, // 0 for admin/platform campaigns
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            IsActive = dto.IsActive,
            MaxUsageCount = dto.MaxUsageCount,
            MaxUsagePerUser = dto.MaxUsagePerUser,
            MinimumOrderAmount = dto.MinimumOrderAmount,
            MinimumQuantity = dto.MinimumQuantity,
            DiscountPercentage = dto.DiscountPercentage,
            DiscountAmount = dto.DiscountAmount,
            MaxDiscountAmount = dto.MaxDiscountAmount,
            BuyQuantity = dto.BuyQuantity,
            GetQuantity = dto.GetQuantity,
            CouponCode = dto.CouponCode?.ToUpper(),
            RequiresCouponCode = dto.RequiresCouponCode,
            Scope = dto.Scope,
            Priority = dto.Priority,
            IsStackable = dto.IsStackable
        };

        _db.Campaigns.Add(campaign);
        await _db.SaveChangesAsync();

        // Add target products
        if (dto.ProductIds?.Any() == true)
        {
            foreach (var productId in dto.ProductIds)
            {
                _db.CampaignProducts.Add(new CampaignProduct
                {
                    CampaignId = campaign.Id,
                    ProductId = productId
                });
            }
        }

        // Add target categories
        if (dto.CategoryIds?.Any() == true)
        {
            foreach (var categoryId in dto.CategoryIds)
            {
                _db.CampaignCategories.Add(new CampaignCategory
                {
                    CampaignId = campaign.Id,
                    CategoryId = categoryId
                });
            }
        }

        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCampaign), new { id = campaign.Id }, new
        {
            Message = "Campaign created successfully",
            CampaignId = campaign.Id
        });
    }

    /// <summary>
    /// Update campaign
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult> UpdateCampaign(int id, [FromBody] UpdateCampaignDto dto)
    {
        var campaign = await _db.Campaigns.FindAsync(id);
        if (campaign == null)
            throw new NotFoundException("Campaign not found");

        // Validate dates if provided
        var startDate = dto.StartDate ?? campaign.StartDate;
        var endDate = dto.EndDate ?? campaign.EndDate;
        if (endDate <= startDate)
            throw new BadRequestException("End date must be after start date");

        // Validate coupon code uniqueness
        if (!string.IsNullOrEmpty(dto.CouponCode) && dto.CouponCode != campaign.CouponCode)
        {
            var codeExists = await _db.Campaigns.AnyAsync(c =>
                c.CouponCode == dto.CouponCode && c.Id != id && !c.IsDeleted);
            if (codeExists)
                throw new BadRequestException("Coupon code already exists");
        }

        // Update fields
        if (!string.IsNullOrEmpty(dto.Name))
            campaign.Name = dto.Name;

        if (dto.Description != null)
            campaign.Description = dto.Description;

        if (dto.Type.HasValue)
            campaign.Type = dto.Type.Value;

        if (dto.StartDate.HasValue)
            campaign.StartDate = dto.StartDate.Value;

        if (dto.EndDate.HasValue)
            campaign.EndDate = dto.EndDate.Value;

        if (dto.IsActive.HasValue)
            campaign.IsActive = dto.IsActive.Value;

        if (dto.MaxUsageCount.HasValue)
            campaign.MaxUsageCount = dto.MaxUsageCount;

        if (dto.MaxUsagePerUser.HasValue)
            campaign.MaxUsagePerUser = dto.MaxUsagePerUser;

        if (dto.MinimumOrderAmount.HasValue)
            campaign.MinimumOrderAmount = dto.MinimumOrderAmount;

        if (dto.MinimumQuantity.HasValue)
            campaign.MinimumQuantity = dto.MinimumQuantity;

        if (dto.DiscountPercentage.HasValue)
            campaign.DiscountPercentage = dto.DiscountPercentage;

        if (dto.DiscountAmount.HasValue)
            campaign.DiscountAmount = dto.DiscountAmount;

        if (dto.MaxDiscountAmount.HasValue)
            campaign.MaxDiscountAmount = dto.MaxDiscountAmount;

        if (dto.CouponCode != null)
            campaign.CouponCode = dto.CouponCode.ToUpper();

        if (dto.RequiresCouponCode.HasValue)
            campaign.RequiresCouponCode = dto.RequiresCouponCode.Value;

        if (dto.Scope.HasValue)
            campaign.Scope = dto.Scope.Value;

        if (dto.Priority.HasValue)
            campaign.Priority = dto.Priority.Value;

        if (dto.IsStackable.HasValue)
            campaign.IsStackable = dto.IsStackable.Value;

        await _db.SaveChangesAsync();

        return Ok(new { Message = "Campaign updated successfully", CampaignId = id });
    }

    /// <summary>
    /// Toggle campaign active status
    /// </summary>
    [HttpPatch("{id:int}/toggle-active")]
    public async Task<ActionResult> ToggleActive(int id)
    {
        var campaign = await _db.Campaigns.FindAsync(id);
        if (campaign == null)
            throw new NotFoundException("Campaign not found");

        campaign.IsActive = !campaign.IsActive;
        await _db.SaveChangesAsync();

        return Ok(new
        {
            Message = campaign.IsActive ? "Campaign activated" : "Campaign deactivated",
            CampaignId = id,
            IsActive = campaign.IsActive
        });
    }

    /// <summary>
    /// Delete campaign
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeleteCampaign(int id)
    {
        var campaign = await _db.Campaigns.FindAsync(id);
        if (campaign == null)
            throw new NotFoundException("Campaign not found");

        _db.Campaigns.Remove(campaign); // Soft delete
        await _db.SaveChangesAsync();

        return Ok(new { Message = "Campaign deleted", CampaignId = id });
    }

    /// <summary>
    /// Update campaign products
    /// </summary>
    [HttpPut("{id:int}/products")]
    public async Task<ActionResult> UpdateCampaignProducts(int id, [FromBody] UpdateCampaignProductsDto dto)
    {
        var campaign = await _db.Campaigns.FindAsync(id);
        if (campaign == null)
            throw new NotFoundException("Campaign not found");

        // Remove existing
        var existing = await _db.CampaignProducts
            .Where(cp => cp.CampaignId == id)
            .ToListAsync();
        _db.CampaignProducts.RemoveRange(existing);

        // Add new
        foreach (var productId in dto.ProductIds)
        {
            _db.CampaignProducts.Add(new CampaignProduct
            {
                CampaignId = id,
                ProductId = productId
            });
        }

        campaign.Scope = CampaignScope.SpecificProducts;
        await _db.SaveChangesAsync();

        return Ok(new { Message = "Campaign products updated", ProductCount = dto.ProductIds.Count });
    }

    /// <summary>
    /// Update campaign categories
    /// </summary>
    [HttpPut("{id:int}/categories")]
    public async Task<ActionResult> UpdateCampaignCategories(int id, [FromBody] UpdateCampaignCategoriesDto dto)
    {
        var campaign = await _db.Campaigns.FindAsync(id);
        if (campaign == null)
            throw new NotFoundException("Campaign not found");

        // Remove existing
        var existing = await _db.CampaignCategories
            .Where(cc => cc.CampaignId == id)
            .ToListAsync();
        _db.CampaignCategories.RemoveRange(existing);

        // Add new
        foreach (var categoryId in dto.CategoryIds)
        {
            _db.CampaignCategories.Add(new CampaignCategory
            {
                CampaignId = id,
                CategoryId = categoryId
            });
        }

        campaign.Scope = CampaignScope.SpecificCategories;
        await _db.SaveChangesAsync();

        return Ok(new { Message = "Campaign categories updated", CategoryCount = dto.CategoryIds.Count });
    }

    /// <summary>
    /// Get campaign usage history
    /// </summary>
    [HttpGet("{id:int}/usages")]
    public async Task<ActionResult> GetCampaignUsages(
        int id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var campaign = await _db.Campaigns.FindAsync(id);
        if (campaign == null)
            throw new NotFoundException("Campaign not found");

        var query = _db.CampaignUsages
            .Where(u => u.CampaignId == id)
            .Include(u => u.User)
            .Include(u => u.Order);

        var totalCount = await query.CountAsync();

        var usages = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new
            {
                u.Id,
                User = u.User != null ? new { u.User.Id, u.User.Name, u.User.Email } : null,
                Order = u.Order != null ? new { u.Order.Id, u.Order.OrderNumber, u.Order.TotalAmount } : null,
                u.DiscountApplied,
                u.CreatedAt
            })
            .ToListAsync();

        return Ok(new
        {
            Items = usages,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
    }

    /// <summary>
    /// Get campaign statistics
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult> GetStatistics()
    {
        var now = DateTime.UtcNow;
        var campaigns = await _db.Campaigns.ToListAsync();
        var usages = await _db.CampaignUsages.ToListAsync();

        var stats = new
        {
            TotalCampaigns = campaigns.Count,
            ActiveCampaigns = campaigns.Count(c => c.IsActive && c.StartDate <= now && c.EndDate >= now),
            ExpiredCampaigns = campaigns.Count(c => c.EndDate < now),
            ScheduledCampaigns = campaigns.Count(c => c.IsActive && c.StartDate > now),

            TotalUsages = usages.Count,
            TotalDiscountGiven = usages.Sum(u => u.DiscountApplied),

            ByType = campaigns
                .GroupBy(c => c.Type)
                .Select(g => new { Type = g.Key.ToString(), Count = g.Count() })
                .ToList(),

            TopCampaigns = campaigns
                .OrderByDescending(c => c.CurrentUsageCount)
                .Take(10)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Type,
                    c.CurrentUsageCount,
                    TotalDiscount = usages.Where(u => u.CampaignId == c.Id).Sum(u => u.DiscountApplied)
                })
                .ToList()
        };

        return Ok(stats);
    }

    /// <summary>
    /// Validate coupon code
    /// </summary>
    [HttpGet("validate-coupon/{code}")]
    public async Task<ActionResult> ValidateCoupon(string code)
    {
        var now = DateTime.UtcNow;

        var campaign = await _db.Campaigns
            .Where(c => c.CouponCode == code.ToUpper() && !c.IsDeleted)
            .FirstOrDefaultAsync();

        if (campaign == null)
            return Ok(new { IsValid = false, Message = "Coupon code not found" });

        if (!campaign.IsActive)
            return Ok(new { IsValid = false, Message = "Campaign is not active" });

        if (campaign.StartDate > now)
            return Ok(new { IsValid = false, Message = "Campaign has not started yet" });

        if (campaign.EndDate < now)
            return Ok(new { IsValid = false, Message = "Campaign has expired" });

        if (campaign.MaxUsageCount.HasValue && campaign.CurrentUsageCount >= campaign.MaxUsageCount)
            return Ok(new { IsValid = false, Message = "Campaign usage limit reached" });

        return Ok(new
        {
            IsValid = true,
            Campaign = new
            {
                campaign.Id,
                campaign.Name,
                campaign.Type,
                campaign.DiscountPercentage,
                campaign.DiscountAmount,
                campaign.MinimumOrderAmount
            }
        });
    }
}

public class UpdateCampaignProductsDto
{
    public required List<int> ProductIds { get; set; }
}

public class UpdateCampaignCategoriesDto
{
    public required List<int> CategoryIds { get; set; }
}
