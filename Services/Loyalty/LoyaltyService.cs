using Exodus.Data;
using Exodus.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Exodus.Services.Loyalty;

public class LoyaltyService : ILoyaltyService
{
    private readonly ApplicationDbContext _db;

    // Puan orani: Her 1 TL = 1 puan, 100 puan = 1 TL
    private const decimal PointValueInTL = 0.01m;
    private const int BasePointsPerTL = 1;

    // Tier esikleri
    private static readonly Dictionary<LoyaltyTier, int> TierThresholds = new()
    {
        { LoyaltyTier.Bronze, 0 },
        { LoyaltyTier.Silver, 5000 },
        { LoyaltyTier.Gold, 20000 },
        { LoyaltyTier.Platinum, 50000 }
    };

    // Tier bonuslari (carpan)
    private static readonly Dictionary<LoyaltyTier, double> TierMultipliers = new()
    {
        { LoyaltyTier.Bronze, 1.0 },
        { LoyaltyTier.Silver, 1.25 },
        { LoyaltyTier.Gold, 1.5 },
        { LoyaltyTier.Platinum, 2.0 }
    };

    public LoyaltyService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<LoyaltyPointDto> GetUserPointsAsync(int userId, CancellationToken ct = default)
    {
        var loyalty = await GetOrCreateLoyaltyAsync(userId, ct);
        var user = await _db.Users.FindAsync(new object[] { userId }, ct);

        return MapToDto(loyalty, user);
    }

    public async Task<LoyaltyPointDto> EarnPointsAsync(int userId, int orderId, decimal orderAmount, CancellationToken ct = default)
    {
        var loyalty = await GetOrCreateLoyaltyAsync(userId, ct);
        var points = await CalculateEarnablePointsAsync(orderAmount, loyalty.Tier, ct);

        loyalty.PendingPoints += points;

        var transaction = new LoyaltyTransaction
        {
            LoyaltyPointId = loyalty.Id,
            Points = points,
            Type = LoyaltyTransactionType.Earned,
            Description = $"Siparis #{orderId} icin {points} puan kazanildi",
            OrderId = orderId,
            ExpiresAt = DateTime.UtcNow.AddYears(1) // 1 yil gecerlilik
        };

        _db.Set<LoyaltyTransaction>().Add(transaction);

        // Pending puanlari hemen available yap (gercek senaryoda siparis teslim sonrasi)
        loyalty.AvailablePoints += points;
        loyalty.TotalPoints += points;
        loyalty.PendingPoints -= points;

        // Tier guncelle
        UpdateTier(loyalty);

        await _db.SaveChangesAsync(ct);
        var user = await _db.Users.FindAsync(new object[] { userId }, ct);
        return MapToDto(loyalty, user);
    }

    public async Task<LoyaltyPointDto> SpendPointsAsync(int userId, int points, int? orderId = null, CancellationToken ct = default)
    {
        var loyalty = await GetOrCreateLoyaltyAsync(userId, ct);

        if (loyalty.AvailablePoints < points)
            throw new InvalidOperationException($"Yetersiz puan. Mevcut: {loyalty.AvailablePoints}, Istenen: {points}");

        loyalty.AvailablePoints -= points;
        loyalty.SpentPoints += points;

        var transaction = new LoyaltyTransaction
        {
            LoyaltyPointId = loyalty.Id,
            Points = -points,
            Type = LoyaltyTransactionType.Spent,
            Description = orderId.HasValue
                ? $"Siparis #{orderId} icin {points} puan kullanildi"
                : $"{points} puan harcandi",
            OrderId = orderId
        };

        _db.Set<LoyaltyTransaction>().Add(transaction);
        await _db.SaveChangesAsync(ct);

        var user = await _db.Users.FindAsync(new object[] { userId }, ct);
        return MapToDto(loyalty, user);
    }

    public async Task<LoyaltyPointDto> RefundPointsAsync(int userId, int points, int orderId, CancellationToken ct = default)
    {
        var loyalty = await GetOrCreateLoyaltyAsync(userId, ct);

        loyalty.AvailablePoints += points;
        loyalty.SpentPoints -= points;

        var transaction = new LoyaltyTransaction
        {
            LoyaltyPointId = loyalty.Id,
            Points = points,
            Type = LoyaltyTransactionType.Refunded,
            Description = $"Siparis #{orderId} iade nedeniyle {points} puan iade edildi",
            OrderId = orderId
        };

        _db.Set<LoyaltyTransaction>().Add(transaction);
        await _db.SaveChangesAsync(ct);

        var user = await _db.Users.FindAsync(new object[] { userId }, ct);
        return MapToDto(loyalty, user);
    }

    public async Task<List<LoyaltyTransactionDto>> GetTransactionHistoryAsync(int userId, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var loyalty = await GetOrCreateLoyaltyAsync(userId, ct);

        var transactions = await _db.Set<LoyaltyTransaction>()
            .Where(t => t.LoyaltyPointId == loyalty.Id)
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return transactions.Select(t => new LoyaltyTransactionDto
        {
            Id = t.Id,
            Points = t.Points,
            Type = t.Type.ToString(),
            Description = t.Description,
            OrderId = t.OrderId,
            ReferenceCode = t.ReferenceCode,
            ExpiresAt = t.ExpiresAt,
            CreatedAt = t.CreatedAt
        }).ToList();
    }

    public Task<decimal> CalculatePointValueAsync(int points, CancellationToken ct = default)
    {
        return Task.FromResult(points * PointValueInTL);
    }

    public Task<int> CalculateEarnablePointsAsync(decimal orderAmount, LoyaltyTier tier, CancellationToken ct = default)
    {
        var basePoints = (int)(orderAmount * BasePointsPerTL);
        var multiplier = TierMultipliers.GetValueOrDefault(tier, 1.0);
        var totalPoints = (int)(basePoints * multiplier);
        return Task.FromResult(totalPoints);
    }

    public async Task<LoyaltyPointDto> AdminAdjustPointsAsync(int userId, int points, string description, CancellationToken ct = default)
    {
        var loyalty = await GetOrCreateLoyaltyAsync(userId, ct);

        loyalty.AvailablePoints += points;
        if (points > 0) loyalty.TotalPoints += points;

        var transaction = new LoyaltyTransaction
        {
            LoyaltyPointId = loyalty.Id,
            Points = points,
            Type = LoyaltyTransactionType.AdminAdjustment,
            Description = description
        };

        _db.Set<LoyaltyTransaction>().Add(transaction);
        UpdateTier(loyalty);
        await _db.SaveChangesAsync(ct);

        var user = await _db.Users.FindAsync(new object[] { userId }, ct);
        return MapToDto(loyalty, user);
    }

    public async Task<List<LoyaltyPointDto>> GetTopUsersAsync(int count = 20, CancellationToken ct = default)
    {
        var loyalties = await _db.Set<LoyaltyPoint>()
            .Include(l => l.User)
            .OrderByDescending(l => l.TotalPoints)
            .Take(count)
            .ToListAsync(ct);

        return loyalties.Select(l => MapToDto(l, l.User)).ToList();
    }

    private async Task<LoyaltyPoint> GetOrCreateLoyaltyAsync(int userId, CancellationToken ct)
    {
        var loyalty = await _db.Set<LoyaltyPoint>()
            .FirstOrDefaultAsync(l => l.UserId == userId, ct);

        if (loyalty == null)
        {
            loyalty = new LoyaltyPoint
            {
                UserId = userId,
                TotalPoints = 0,
                AvailablePoints = 0,
                SpentPoints = 0,
                PendingPoints = 0,
                Tier = LoyaltyTier.Bronze
            };
            _db.Set<LoyaltyPoint>().Add(loyalty);
            await _db.SaveChangesAsync(ct);
        }

        return loyalty;
    }

    private static void UpdateTier(LoyaltyPoint loyalty)
    {
        if (loyalty.TotalPoints >= TierThresholds[LoyaltyTier.Platinum])
            loyalty.Tier = LoyaltyTier.Platinum;
        else if (loyalty.TotalPoints >= TierThresholds[LoyaltyTier.Gold])
            loyalty.Tier = LoyaltyTier.Gold;
        else if (loyalty.TotalPoints >= TierThresholds[LoyaltyTier.Silver])
            loyalty.Tier = LoyaltyTier.Silver;
        else
            loyalty.Tier = LoyaltyTier.Bronze;
    }

    private static int GetPointsToNextTier(LoyaltyPoint loyalty)
    {
        return loyalty.Tier switch
        {
            LoyaltyTier.Bronze => TierThresholds[LoyaltyTier.Silver] - loyalty.TotalPoints,
            LoyaltyTier.Silver => TierThresholds[LoyaltyTier.Gold] - loyalty.TotalPoints,
            LoyaltyTier.Gold => TierThresholds[LoyaltyTier.Platinum] - loyalty.TotalPoints,
            _ => 0
        };
    }

    private static LoyaltyPointDto MapToDto(LoyaltyPoint loyalty, Exodus.Models.Entities.Users? user)
    {
        return new LoyaltyPointDto
        {
            UserId = loyalty.UserId,
            UserName = user?.Name ?? string.Empty,
            TotalPoints = loyalty.TotalPoints,
            AvailablePoints = loyalty.AvailablePoints,
            SpentPoints = loyalty.SpentPoints,
            PendingPoints = loyalty.PendingPoints,
            Tier = loyalty.Tier.ToString(),
            PointValue = loyalty.AvailablePoints * PointValueInTL,
            PointsToNextTier = GetPointsToNextTier(loyalty)
        };
    }
}
