using FarmazonDemo.Data;
using FarmazonDemo.Models.Dto.Campaign;
using FarmazonDemo.Models.Entities;
using FarmazonDemo.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace FarmazonDemo.Services.Campaigns;

public class CampaignService : ICampaignService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<CampaignService> _logger;

    public CampaignService(ApplicationDbContext db, ILogger<CampaignService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<CampaignDto> CreateAsync(int sellerId, CreateCampaignDto dto, CancellationToken ct = default)
    {
        // Validate dates
        if (dto.EndDate <= dto.StartDate)
            throw new ValidationException("End date must be after start date");

        // Validate campaign type requirements
        ValidateCampaignTypeRequirements(dto);

        // Check coupon code uniqueness
        if (!string.IsNullOrEmpty(dto.CouponCode))
        {
            var exists = await _db.Campaigns.AnyAsync(c => c.CouponCode == dto.CouponCode, ct);
            if (exists)
                throw new ValidationException("Coupon code already exists");
        }

        var campaign = new Campaign
        {
            Name = dto.Name,
            Description = dto.Description,
            Type = dto.Type,
            SellerId = sellerId,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            IsActive = true,
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
        await _db.SaveChangesAsync(ct);

        // Add product/listing/category associations
        await AddCampaignTargetsAsync(campaign.Id, dto.ProductIds, dto.ListingIds, dto.CategoryIds, ct);

        return await GetByIdAsync(campaign.Id, ct);
    }

    public async Task<CampaignDto> GetByIdAsync(int campaignId, CancellationToken ct = default)
    {
        var campaign = await _db.Campaigns
            .Include(c => c.Seller)
            .Include(c => c.CampaignProducts)
            .Include(c => c.CampaignCategories)
            .FirstOrDefaultAsync(c => c.Id == campaignId, ct);

        if (campaign is null)
            throw new NotFoundException($"Campaign not found. Id={campaignId}");

        return MapToDto(campaign);
    }

    public async Task<List<CampaignDto>> GetBySellerAsync(int sellerId, bool includeInactive = false, CancellationToken ct = default)
    {
        var query = _db.Campaigns
            .Include(c => c.Seller)
            .Include(c => c.CampaignProducts)
            .Include(c => c.CampaignCategories)
            .Where(c => c.SellerId == sellerId);

        if (!includeInactive)
        {
            query = query.Where(c => c.IsActive);
        }

        var campaigns = await query
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);

        return campaigns.Select(MapToDto).ToList();
    }

    public async Task<CampaignDto> UpdateAsync(int campaignId, int sellerId, UpdateCampaignDto dto, CancellationToken ct = default)
    {
        var campaign = await _db.Campaigns
            .Include(c => c.CampaignProducts)
            .Include(c => c.CampaignCategories)
            .FirstOrDefaultAsync(c => c.Id == campaignId && c.SellerId == sellerId, ct);

        if (campaign is null)
            throw new NotFoundException($"Campaign not found or access denied. Id={campaignId}");

        // Update fields
        if (dto.Name != null) campaign.Name = dto.Name;
        if (dto.Description != null) campaign.Description = dto.Description;
        if (dto.StartDate.HasValue) campaign.StartDate = dto.StartDate.Value;
        if (dto.EndDate.HasValue) campaign.EndDate = dto.EndDate.Value;
        if (dto.IsActive.HasValue) campaign.IsActive = dto.IsActive.Value;
        if (dto.MaxUsageCount.HasValue) campaign.MaxUsageCount = dto.MaxUsageCount;
        if (dto.MaxUsagePerUser.HasValue) campaign.MaxUsagePerUser = dto.MaxUsagePerUser;
        if (dto.MinimumOrderAmount.HasValue) campaign.MinimumOrderAmount = dto.MinimumOrderAmount;
        if (dto.MinimumQuantity.HasValue) campaign.MinimumQuantity = dto.MinimumQuantity;
        if (dto.DiscountPercentage.HasValue) campaign.DiscountPercentage = dto.DiscountPercentage;
        if (dto.DiscountAmount.HasValue) campaign.DiscountAmount = dto.DiscountAmount;
        if (dto.MaxDiscountAmount.HasValue) campaign.MaxDiscountAmount = dto.MaxDiscountAmount;
        if (dto.BuyQuantity.HasValue) campaign.BuyQuantity = dto.BuyQuantity;
        if (dto.GetQuantity.HasValue) campaign.GetQuantity = dto.GetQuantity;
        if (dto.CouponCode != null) campaign.CouponCode = dto.CouponCode.ToUpper();
        if (dto.RequiresCouponCode.HasValue) campaign.RequiresCouponCode = dto.RequiresCouponCode.Value;
        if (dto.Priority.HasValue) campaign.Priority = dto.Priority.Value;
        if (dto.IsStackable.HasValue) campaign.IsStackable = dto.IsStackable.Value;

        // Update targets if provided
        if (dto.ProductIds != null || dto.ListingIds != null || dto.CategoryIds != null)
        {
            // Remove existing
            _db.CampaignProducts.RemoveRange(campaign.CampaignProducts);
            _db.CampaignCategories.RemoveRange(campaign.CampaignCategories);
            await _db.SaveChangesAsync(ct);

            // Add new
            await AddCampaignTargetsAsync(campaign.Id, dto.ProductIds, dto.ListingIds, dto.CategoryIds, ct);
        }

        await _db.SaveChangesAsync(ct);
        return await GetByIdAsync(campaignId, ct);
    }

    public async Task DeleteAsync(int campaignId, int sellerId, CancellationToken ct = default)
    {
        var campaign = await _db.Campaigns
            .FirstOrDefaultAsync(c => c.Id == campaignId && c.SellerId == sellerId, ct);

        if (campaign is null)
            throw new NotFoundException($"Campaign not found or access denied. Id={campaignId}");

        _db.Campaigns.Remove(campaign); // Soft delete via BaseEntity
        await _db.SaveChangesAsync(ct);
    }

    public async Task<CampaignDto> ToggleActiveAsync(int campaignId, int sellerId, CancellationToken ct = default)
    {
        var campaign = await _db.Campaigns
            .FirstOrDefaultAsync(c => c.Id == campaignId && c.SellerId == sellerId, ct);

        if (campaign is null)
            throw new NotFoundException($"Campaign not found or access denied. Id={campaignId}");

        campaign.IsActive = !campaign.IsActive;
        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(campaignId, ct);
    }

    public async Task<CampaignApplicationResult> CalculateDiscountAsync(int userId, List<CartItemForCampaign> items, string? couponCode = null, CancellationToken ct = default)
    {
        var result = new CampaignApplicationResult
        {
            OriginalTotal = items.Sum(i => i.UnitPrice * i.Quantity)
        };

        // Find applicable campaign
        Campaign? campaign = null;

        if (!string.IsNullOrEmpty(couponCode))
        {
            campaign = await _db.Campaigns
                .Include(c => c.CampaignProducts)
                .Include(c => c.CampaignCategories)
                .FirstOrDefaultAsync(c =>
                    c.CouponCode == couponCode.ToUpper() &&
                    c.IsActive &&
                    c.StartDate <= DateTime.UtcNow &&
                    c.EndDate >= DateTime.UtcNow, ct);

            if (campaign == null)
            {
                result.Success = false;
                result.Message = "Gecersiz veya suresi dolmus kupon kodu";
                result.FinalTotal = result.OriginalTotal;
                return result;
            }
        }
        else
        {
            // Find best automatic campaign
            var sellerIds = items.Select(i => i.SellerId).Distinct().ToList();
            var campaigns = await _db.Campaigns
                .Include(c => c.CampaignProducts)
                .Include(c => c.CampaignCategories)
                .Where(c =>
                    c.IsActive &&
                    !c.RequiresCouponCode &&
                    c.StartDate <= DateTime.UtcNow &&
                    c.EndDate >= DateTime.UtcNow &&
                    sellerIds.Contains(c.SellerId))
                .OrderByDescending(c => c.Priority)
                .ToListAsync(ct);

            campaign = campaigns.FirstOrDefault(c => IsCampaignApplicable(c, items, userId));
        }

        if (campaign == null)
        {
            result.Success = false;
            result.Message = "Uygulanabilir kampanya bulunamadi";
            result.FinalTotal = result.OriginalTotal;
            return result;
        }

        // Check usage limits
        if (!await CanUseCampaignAsync(campaign, userId, ct))
        {
            result.Success = false;
            result.Message = "Kampanya kullanim limitine ulasildi";
            result.FinalTotal = result.OriginalTotal;
            return result;
        }

        // Check minimum requirements
        if (campaign.MinimumOrderAmount.HasValue && result.OriginalTotal < campaign.MinimumOrderAmount.Value)
        {
            result.Success = false;
            result.Message = $"Minimum siparis tutari {campaign.MinimumOrderAmount:N2} TL olmalidir";
            result.FinalTotal = result.OriginalTotal;
            return result;
        }

        var totalQuantity = items.Sum(i => i.Quantity);
        if (campaign.MinimumQuantity.HasValue && totalQuantity < campaign.MinimumQuantity.Value)
        {
            result.Success = false;
            result.Message = $"Minimum {campaign.MinimumQuantity} adet urun olmalidir";
            result.FinalTotal = result.OriginalTotal;
            return result;
        }

        // Calculate discount based on campaign type
        var applicableItems = GetApplicableItems(campaign, items);
        var discountResult = CalculateCampaignDiscount(campaign, applicableItems);

        result.Success = true;
        result.CampaignId = campaign.Id;
        result.CampaignName = campaign.Name;
        result.CampaignType = campaign.Type;
        result.DiscountAmount = discountResult.TotalDiscount;
        result.FinalTotal = result.OriginalTotal - result.DiscountAmount;
        result.ItemDiscounts = discountResult.ItemDiscounts;
        result.Message = $"Kampanya uygulandı: {campaign.Name}";

        return result;
    }

    public async Task<CampaignApplicationResult> ApplyToOrderAsync(int userId, int orderId, int campaignId, CancellationToken ct = default)
    {
        var campaign = await _db.Campaigns.FindAsync(new object[] { campaignId }, ct);
        if (campaign == null)
            throw new NotFoundException($"Campaign not found. Id={campaignId}");

        var order = await _db.Orders.FindAsync(new object[] { orderId }, ct);
        if (order == null)
            throw new NotFoundException($"Order not found. Id={orderId}");

        // Record usage
        var usage = new CampaignUsage
        {
            CampaignId = campaignId,
            UserId = userId,
            OrderId = orderId,
            DiscountApplied = 0, // Will be updated
            UsedAt = DateTime.UtcNow
        };

        _db.CampaignUsages.Add(usage);
        campaign.CurrentUsageCount++;
        await _db.SaveChangesAsync(ct);

        return new CampaignApplicationResult
        {
            Success = true,
            CampaignId = campaign.Id,
            CampaignName = campaign.Name
        };
    }

    public async Task<List<CampaignDto>> GetApplicableCampaignsAsync(int userId, List<CartItemForCampaign> items, CancellationToken ct = default)
    {
        var sellerIds = items.Select(i => i.SellerId).Distinct().ToList();

        var campaigns = await _db.Campaigns
            .Include(c => c.Seller)
            .Include(c => c.CampaignProducts)
            .Include(c => c.CampaignCategories)
            .Where(c =>
                c.IsActive &&
                c.StartDate <= DateTime.UtcNow &&
                c.EndDate >= DateTime.UtcNow &&
                sellerIds.Contains(c.SellerId))
            .ToListAsync(ct);

        var applicableCampaigns = new List<CampaignDto>();

        foreach (var campaign in campaigns)
        {
            if (IsCampaignApplicable(campaign, items, userId) && await CanUseCampaignAsync(campaign, userId, ct))
            {
                applicableCampaigns.Add(MapToDto(campaign));
            }
        }

        return applicableCampaigns.OrderByDescending(c => c.Priority).ToList();
    }

    public async Task<CampaignDto?> ValidateCouponCodeAsync(string couponCode, int userId, CancellationToken ct = default)
    {
        var campaign = await _db.Campaigns
            .Include(c => c.Seller)
            .FirstOrDefaultAsync(c =>
                c.CouponCode == couponCode.ToUpper() &&
                c.IsActive &&
                c.StartDate <= DateTime.UtcNow &&
                c.EndDate >= DateTime.UtcNow, ct);

        if (campaign == null)
            return null;

        if (!await CanUseCampaignAsync(campaign, userId, ct))
            return null;

        return MapToDto(campaign);
    }

    public async Task<CampaignStatisticsDto> GetStatisticsAsync(int campaignId, int sellerId, CancellationToken ct = default)
    {
        var campaign = await _db.Campaigns
            .FirstOrDefaultAsync(c => c.Id == campaignId && c.SellerId == sellerId, ct);

        if (campaign == null)
            throw new NotFoundException($"Campaign not found or access denied. Id={campaignId}");

        var usages = await _db.CampaignUsages
            .Where(u => u.CampaignId == campaignId)
            .ToListAsync(ct);

        var dailyUsage = usages
            .GroupBy(u => u.UsedAt.Date)
            .Select(g => new DailyUsageDto
            {
                Date = g.Key,
                UsageCount = g.Count(),
                DiscountAmount = g.Sum(u => u.DiscountApplied)
            })
            .OrderByDescending(d => d.Date)
            .Take(30)
            .ToList();

        return new CampaignStatisticsDto
        {
            CampaignId = campaignId,
            CampaignName = campaign.Name,
            TotalUsageCount = usages.Count,
            UniqueUsersCount = usages.Select(u => u.UserId).Distinct().Count(),
            TotalDiscountGiven = usages.Sum(u => u.DiscountApplied),
            AverageDiscountPerUse = usages.Count > 0 ? usages.Average(u => u.DiscountApplied) : 0,
            DailyUsage = dailyUsage
        };
    }

    public async Task<List<CampaignUsageDto>> GetUsageHistoryAsync(int campaignId, int sellerId, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var campaign = await _db.Campaigns
            .FirstOrDefaultAsync(c => c.Id == campaignId && c.SellerId == sellerId, ct);

        if (campaign == null)
            throw new NotFoundException($"Campaign not found or access denied. Id={campaignId}");

        var usages = await _db.CampaignUsages
            .Include(u => u.User)
            .Include(u => u.Order)
            .Where(u => u.CampaignId == campaignId)
            .OrderByDescending(u => u.UsedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return usages.Select(u => new CampaignUsageDto
        {
            Id = u.Id,
            CampaignId = u.CampaignId,
            CampaignName = campaign.Name,
            UserId = u.UserId,
            UserName = u.User?.FullName,
            OrderId = u.OrderId,
            OrderNumber = u.Order?.OrderNumber,
            DiscountApplied = u.DiscountApplied,
            UsedAt = u.UsedAt
        }).ToList();
    }

    public async Task<List<CampaignDto>> GetActiveCampaignsAsync(int? categoryId = null, CancellationToken ct = default)
    {
        var query = _db.Campaigns
            .Include(c => c.Seller)
            .Include(c => c.CampaignProducts)
            .Include(c => c.CampaignCategories)
            .Where(c =>
                c.IsActive &&
                !c.RequiresCouponCode &&
                c.StartDate <= DateTime.UtcNow &&
                c.EndDate >= DateTime.UtcNow);

        if (categoryId.HasValue)
        {
            query = query.Where(c =>
                c.Scope == CampaignScope.AllProducts ||
                c.CampaignCategories.Any(cc => cc.CategoryId == categoryId.Value));
        }

        var campaigns = await query
            .OrderByDescending(c => c.Priority)
            .Take(20)
            .ToListAsync(ct);

        return campaigns.Select(MapToDto).ToList();
    }

    // Private helpers

    private async Task AddCampaignTargetsAsync(int campaignId, List<int>? productIds, List<int>? listingIds, List<int>? categoryIds, CancellationToken ct)
    {
        if (productIds?.Any() == true)
        {
            foreach (var productId in productIds)
            {
                _db.CampaignProducts.Add(new CampaignProduct { CampaignId = campaignId, ProductId = productId });
            }
        }

        if (listingIds?.Any() == true)
        {
            foreach (var listingId in listingIds)
            {
                _db.CampaignProducts.Add(new CampaignProduct { CampaignId = campaignId, ListingId = listingId });
            }
        }

        if (categoryIds?.Any() == true)
        {
            foreach (var categoryId in categoryIds)
            {
                _db.CampaignCategories.Add(new CampaignCategory { CampaignId = campaignId, CategoryId = categoryId });
            }
        }

        await _db.SaveChangesAsync(ct);
    }

    private void ValidateCampaignTypeRequirements(CreateCampaignDto dto)
    {
        switch (dto.Type)
        {
            case CampaignType.PercentageDiscount:
                if (!dto.DiscountPercentage.HasValue || dto.DiscountPercentage <= 0 || dto.DiscountPercentage > 100)
                    throw new ValidationException("Percentage discount requires valid discount percentage (1-100)");
                break;

            case CampaignType.FixedAmountDiscount:
                if (!dto.DiscountAmount.HasValue || dto.DiscountAmount <= 0)
                    throw new ValidationException("Fixed amount discount requires valid discount amount");
                break;

            case CampaignType.BuyXGetYFree:
                if (!dto.BuyQuantity.HasValue || dto.BuyQuantity <= 0 || !dto.GetQuantity.HasValue || dto.GetQuantity <= 0)
                    throw new ValidationException("BOGO campaign requires BuyQuantity and GetQuantity");
                break;

            case CampaignType.BuyXPayY:
                if (!dto.BuyQuantity.HasValue || dto.BuyQuantity <= 0 || !dto.GetQuantity.HasValue || dto.GetQuantity <= 0)
                    throw new ValidationException("BuyXPayY campaign requires BuyQuantity and GetQuantity (pay amount)");
                if (dto.GetQuantity >= dto.BuyQuantity)
                    throw new ValidationException("GetQuantity (pay amount) must be less than BuyQuantity");
                break;
        }
    }

    private bool IsCampaignApplicable(Campaign campaign, List<CartItemForCampaign> items, int userId)
    {
        // Filter items by seller
        var sellerItems = items.Where(i => i.SellerId == campaign.SellerId).ToList();
        if (!sellerItems.Any()) return false;

        // Check scope
        switch (campaign.Scope)
        {
            case CampaignScope.AllProducts:
                return true;

            case CampaignScope.SpecificProducts:
                var productIds = campaign.CampaignProducts.Where(cp => cp.ProductId.HasValue).Select(cp => cp.ProductId!.Value).ToList();
                return sellerItems.Any(i => productIds.Contains(i.ProductId));

            case CampaignScope.SpecificListings:
                var listingIds = campaign.CampaignProducts.Where(cp => cp.ListingId.HasValue).Select(cp => cp.ListingId!.Value).ToList();
                return sellerItems.Any(i => listingIds.Contains(i.ListingId));

            case CampaignScope.SpecificCategories:
                var categoryIds = campaign.CampaignCategories.Select(cc => cc.CategoryId).ToList();
                return sellerItems.Any(i => i.CategoryId.HasValue && categoryIds.Contains(i.CategoryId.Value));

            default:
                return false;
        }
    }

    private async Task<bool> CanUseCampaignAsync(Campaign campaign, int userId, CancellationToken ct)
    {
        // Check max usage
        if (campaign.MaxUsageCount.HasValue && campaign.CurrentUsageCount >= campaign.MaxUsageCount.Value)
            return false;

        // Check per-user limit
        if (campaign.MaxUsagePerUser.HasValue)
        {
            var userUsageCount = await _db.CampaignUsages
                .CountAsync(u => u.CampaignId == campaign.Id && u.UserId == userId, ct);

            if (userUsageCount >= campaign.MaxUsagePerUser.Value)
                return false;
        }

        return true;
    }

    private List<CartItemForCampaign> GetApplicableItems(Campaign campaign, List<CartItemForCampaign> items)
    {
        var sellerItems = items.Where(i => i.SellerId == campaign.SellerId).ToList();

        switch (campaign.Scope)
        {
            case CampaignScope.AllProducts:
                return sellerItems;

            case CampaignScope.SpecificProducts:
                var productIds = campaign.CampaignProducts.Where(cp => cp.ProductId.HasValue).Select(cp => cp.ProductId!.Value).ToHashSet();
                return sellerItems.Where(i => productIds.Contains(i.ProductId)).ToList();

            case CampaignScope.SpecificListings:
                var listingIds = campaign.CampaignProducts.Where(cp => cp.ListingId.HasValue).Select(cp => cp.ListingId!.Value).ToHashSet();
                return sellerItems.Where(i => listingIds.Contains(i.ListingId)).ToList();

            case CampaignScope.SpecificCategories:
                var categoryIds = campaign.CampaignCategories.Select(cc => cc.CategoryId).ToHashSet();
                return sellerItems.Where(i => i.CategoryId.HasValue && categoryIds.Contains(i.CategoryId.Value)).ToList();

            default:
                return new List<CartItemForCampaign>();
        }
    }

    private (decimal TotalDiscount, List<CampaignItemDiscount> ItemDiscounts) CalculateCampaignDiscount(Campaign campaign, List<CartItemForCampaign> items)
    {
        var itemDiscounts = new List<CampaignItemDiscount>();
        decimal totalDiscount = 0;

        switch (campaign.Type)
        {
            case CampaignType.PercentageDiscount:
                foreach (var item in items)
                {
                    var lineTotal = item.UnitPrice * item.Quantity;
                    var discount = lineTotal * (campaign.DiscountPercentage!.Value / 100);

                    if (campaign.MaxDiscountAmount.HasValue)
                        discount = Math.Min(discount, campaign.MaxDiscountAmount.Value);

                    itemDiscounts.Add(new CampaignItemDiscount
                    {
                        ListingId = item.ListingId,
                        ProductName = item.ProductName,
                        Quantity = item.Quantity,
                        OriginalPrice = lineTotal,
                        DiscountAmount = discount,
                        DiscountedPrice = lineTotal - discount
                    });
                    totalDiscount += discount;
                }
                break;

            case CampaignType.FixedAmountDiscount:
                var totalAmount = items.Sum(i => i.UnitPrice * i.Quantity);
                totalDiscount = Math.Min(campaign.DiscountAmount!.Value, totalAmount);

                // Distribute discount proportionally
                foreach (var item in items)
                {
                    var lineTotal = item.UnitPrice * item.Quantity;
                    var proportion = lineTotal / totalAmount;
                    var discount = totalDiscount * proportion;

                    itemDiscounts.Add(new CampaignItemDiscount
                    {
                        ListingId = item.ListingId,
                        ProductName = item.ProductName,
                        Quantity = item.Quantity,
                        OriginalPrice = lineTotal,
                        DiscountAmount = discount,
                        DiscountedPrice = lineTotal - discount
                    });
                }
                break;

            case CampaignType.BuyXGetYFree:
                // 1 Al 1 Bedava logic
                var buyQty = campaign.BuyQuantity!.Value;
                var freeQty = campaign.GetQuantity!.Value;

                foreach (var item in items)
                {
                    var sets = item.Quantity / (buyQty + freeQty);
                    var freeItems = sets * freeQty;
                    var discount = freeItems * item.UnitPrice;

                    itemDiscounts.Add(new CampaignItemDiscount
                    {
                        ListingId = item.ListingId,
                        ProductName = item.ProductName,
                        Quantity = item.Quantity,
                        OriginalPrice = item.UnitPrice * item.Quantity,
                        DiscountAmount = discount,
                        DiscountedPrice = (item.UnitPrice * item.Quantity) - discount,
                        FreeQuantity = (int)freeItems
                    });
                    totalDiscount += discount;
                }
                break;

            case CampaignType.BuyXPayY:
                // 3 Al 2 Ode logic
                var buyCount = campaign.BuyQuantity!.Value;
                var payCount = campaign.GetQuantity!.Value;

                foreach (var item in items)
                {
                    var sets = item.Quantity / buyCount;
                    var discountPerSet = (buyCount - payCount) * item.UnitPrice;
                    var discount = sets * discountPerSet;

                    itemDiscounts.Add(new CampaignItemDiscount
                    {
                        ListingId = item.ListingId,
                        ProductName = item.ProductName,
                        Quantity = item.Quantity,
                        OriginalPrice = item.UnitPrice * item.Quantity,
                        DiscountAmount = discount,
                        DiscountedPrice = (item.UnitPrice * item.Quantity) - discount,
                        FreeQuantity = (int)(sets * (buyCount - payCount))
                    });
                    totalDiscount += discount;
                }
                break;

            case CampaignType.FreeShipping:
                // Free shipping is handled separately
                break;

            case CampaignType.MinimumAmountDiscount:
                var orderTotal = items.Sum(i => i.UnitPrice * i.Quantity);
                if (orderTotal >= campaign.MinimumOrderAmount)
                {
                    totalDiscount = campaign.DiscountPercentage.HasValue
                        ? orderTotal * (campaign.DiscountPercentage.Value / 100)
                        : campaign.DiscountAmount ?? 0;

                    if (campaign.MaxDiscountAmount.HasValue)
                        totalDiscount = Math.Min(totalDiscount, campaign.MaxDiscountAmount.Value);
                }
                break;
        }

        return (totalDiscount, itemDiscounts);
    }

    private CampaignDto MapToDto(Campaign c)
    {
        var now = DateTime.UtcNow;
        return new CampaignDto
        {
            Id = c.Id,
            Name = c.Name,
            Description = c.Description,
            Type = c.Type,
            SellerId = c.SellerId,
            SellerName = c.Seller?.FullName,
            StartDate = c.StartDate,
            EndDate = c.EndDate,
            IsActive = c.IsActive,
            IsCurrentlyValid = c.IsActive && c.StartDate <= now && c.EndDate >= now,
            MaxUsageCount = c.MaxUsageCount,
            MaxUsagePerUser = c.MaxUsagePerUser,
            CurrentUsageCount = c.CurrentUsageCount,
            MinimumOrderAmount = c.MinimumOrderAmount,
            MinimumQuantity = c.MinimumQuantity,
            DiscountPercentage = c.DiscountPercentage,
            DiscountAmount = c.DiscountAmount,
            MaxDiscountAmount = c.MaxDiscountAmount,
            BuyQuantity = c.BuyQuantity,
            GetQuantity = c.GetQuantity,
            CouponCode = c.CouponCode,
            RequiresCouponCode = c.RequiresCouponCode,
            Scope = c.Scope,
            Priority = c.Priority,
            IsStackable = c.IsStackable,
            ProductIds = c.CampaignProducts?.Where(cp => cp.ProductId.HasValue).Select(cp => cp.ProductId!.Value).ToList(),
            ListingIds = c.CampaignProducts?.Where(cp => cp.ListingId.HasValue).Select(cp => cp.ListingId!.Value).ToList(),
            CategoryIds = c.CampaignCategories?.Select(cc => cc.CategoryId).ToList(),
            CreatedAt = c.CreatedAt
        };
    }
}
