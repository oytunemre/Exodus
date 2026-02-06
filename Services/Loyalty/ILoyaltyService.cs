using FarmazonDemo.Models.Entities;

namespace FarmazonDemo.Services.Loyalty;

public interface ILoyaltyService
{
    Task<LoyaltyPointDto> GetUserPointsAsync(int userId, CancellationToken ct = default);
    Task<LoyaltyPointDto> EarnPointsAsync(int userId, int orderId, decimal orderAmount, CancellationToken ct = default);
    Task<LoyaltyPointDto> SpendPointsAsync(int userId, int points, int? orderId = null, CancellationToken ct = default);
    Task<LoyaltyPointDto> RefundPointsAsync(int userId, int points, int orderId, CancellationToken ct = default);
    Task<List<LoyaltyTransactionDto>> GetTransactionHistoryAsync(int userId, int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<decimal> CalculatePointValueAsync(int points, CancellationToken ct = default);
    Task<int> CalculateEarnablePointsAsync(decimal orderAmount, LoyaltyTier tier, CancellationToken ct = default);

    // Admin
    Task<LoyaltyPointDto> AdminAdjustPointsAsync(int userId, int points, string description, CancellationToken ct = default);
    Task<List<LoyaltyPointDto>> GetTopUsersAsync(int count = 20, CancellationToken ct = default);
}

public class LoyaltyPointDto
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int TotalPoints { get; set; }
    public int AvailablePoints { get; set; }
    public int SpentPoints { get; set; }
    public int PendingPoints { get; set; }
    public string Tier { get; set; } = string.Empty;
    public decimal PointValue { get; set; } // TL karsiligi
    public int PointsToNextTier { get; set; }
}

public class LoyaltyTransactionDto
{
    public int Id { get; set; }
    public int Points { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int? OrderId { get; set; }
    public string? ReferenceCode { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
