using Exodus.Models.Dto.Campaign;

namespace Exodus.Services.Campaigns;

public interface ICampaignService
{
    // CRUD Operations (Seller)
    Task<CampaignDto> CreateAsync(int sellerId, CreateCampaignDto dto, CancellationToken ct = default);
    Task<CampaignDto> GetByIdAsync(int campaignId, CancellationToken ct = default);
    Task<List<CampaignDto>> GetBySellerAsync(int sellerId, bool includeInactive = false, CancellationToken ct = default);
    Task<CampaignDto> UpdateAsync(int campaignId, int sellerId, UpdateCampaignDto dto, CancellationToken ct = default);
    Task DeleteAsync(int campaignId, int sellerId, CancellationToken ct = default);
    Task<CampaignDto> ToggleActiveAsync(int campaignId, int sellerId, CancellationToken ct = default);

    // Campaign Application
    Task<CampaignApplicationResult> CalculateDiscountAsync(int userId, List<CartItemForCampaign> items, string? couponCode = null, CancellationToken ct = default);
    Task<CampaignApplicationResult> ApplyToOrderAsync(int userId, int orderId, int campaignId, CancellationToken ct = default);
    Task<List<CampaignDto>> GetApplicableCampaignsAsync(int userId, List<CartItemForCampaign> items, CancellationToken ct = default);
    Task<CampaignDto?> ValidateCouponCodeAsync(string couponCode, int userId, CancellationToken ct = default);

    // Statistics & Usage
    Task<CampaignStatisticsDto> GetStatisticsAsync(int campaignId, int sellerId, CancellationToken ct = default);
    Task<List<CampaignUsageDto>> GetUsageHistoryAsync(int campaignId, int sellerId, int page = 1, int pageSize = 20, CancellationToken ct = default);

    // Public campaigns (for customers)
    Task<List<CampaignDto>> GetActiveCampaignsAsync(int? categoryId = null, CancellationToken ct = default);
}

public class CartItemForCampaign
{
    public int ListingId { get; set; }
    public int ProductId { get; set; }
    public int? CategoryId { get; set; }
    public int SellerId { get; set; }
    public string? ProductName { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}
