using Exodus.Data;
using Exodus.Models.Entities;
using Exodus.Services.Loyalty;
using Exodus.Tests.Helpers;
using FluentAssertions;
using Xunit;

namespace Exodus.Tests.Services;

public class LoyaltyServiceTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly LoyaltyService _service;

    public LoyaltyServiceTests()
    {
        _db = TestDbContextFactory.Create();
        _service = new LoyaltyService(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    private async Task<Users> SeedUserAsync(string name = "Test User")
    {
        var user = new Users
        {
            Name = name,
            Email = $"{name.Replace(" ", "").ToLower()}@test.com",
            Password = "pass",
            Username = name.Replace(" ", "").ToLower()
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    #region GetUserPointsAsync

    [Fact]
    public async Task GetUserPointsAsync_WhenNewUser_ShouldCreateDefaultLoyaltyPoint()
    {
        var user = await SeedUserAsync();

        var result = await _service.GetUserPointsAsync(user.Id);

        result.UserId.Should().Be(user.Id);
        result.TotalPoints.Should().Be(0);
        result.AvailablePoints.Should().Be(0);
        result.SpentPoints.Should().Be(0);
        result.PendingPoints.Should().Be(0);
        result.Tier.Should().Be("Bronze");
    }

    [Fact]
    public async Task GetUserPointsAsync_WhenExistingUser_ShouldReturnCurrentPoints()
    {
        var user = await SeedUserAsync();

        // First call creates the loyalty record
        await _service.GetUserPointsAsync(user.Id);
        // Earn some points
        await _service.EarnPointsAsync(user.Id, 1, 100m);

        var result = await _service.GetUserPointsAsync(user.Id);
        result.TotalPoints.Should().BeGreaterThan(0);
    }

    #endregion

    #region EarnPointsAsync

    [Fact]
    public async Task EarnPointsAsync_ShouldAddPointsToUser()
    {
        var user = await SeedUserAsync();

        var result = await _service.EarnPointsAsync(user.Id, orderId: 1, orderAmount: 100m);

        result.TotalPoints.Should().BeGreaterThan(0);
        result.AvailablePoints.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task EarnPointsAsync_BronzeTier_ShouldEarnBasePoints()
    {
        var user = await SeedUserAsync();

        // Bronze = 1x multiplier, 1 point per TL
        var result = await _service.EarnPointsAsync(user.Id, orderId: 1, orderAmount: 100m);

        result.TotalPoints.Should().Be(100);
        result.AvailablePoints.Should().Be(100);
    }

    [Fact]
    public async Task EarnPointsAsync_ShouldCreateTransaction()
    {
        var user = await SeedUserAsync();

        await _service.EarnPointsAsync(user.Id, orderId: 1, orderAmount: 100m);

        var transactions = await _service.GetTransactionHistoryAsync(user.Id);
        transactions.Should().HaveCount(1);
        transactions[0].Type.Should().Be("Earned");
        transactions[0].Points.Should().Be(100);
    }

    [Fact]
    public async Task EarnPointsAsync_MultipleOrders_ShouldAccumulate()
    {
        var user = await SeedUserAsync();

        await _service.EarnPointsAsync(user.Id, 1, 100m);
        await _service.EarnPointsAsync(user.Id, 2, 200m);

        var result = await _service.GetUserPointsAsync(user.Id);
        result.TotalPoints.Should().Be(300);
        result.AvailablePoints.Should().Be(300);
    }

    #endregion

    #region SpendPointsAsync

    [Fact]
    public async Task SpendPointsAsync_WithSufficientPoints_ShouldDeductPoints()
    {
        var user = await SeedUserAsync();
        await _service.EarnPointsAsync(user.Id, 1, 100m);

        var result = await _service.SpendPointsAsync(user.Id, 50, orderId: 2);

        result.AvailablePoints.Should().Be(50);
        result.SpentPoints.Should().Be(50);
    }

    [Fact]
    public async Task SpendPointsAsync_WithInsufficientPoints_ShouldThrowException()
    {
        var user = await SeedUserAsync();
        await _service.EarnPointsAsync(user.Id, 1, 100m);

        var act = () => _service.SpendPointsAsync(user.Id, 200);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Yetersiz puan*");
    }

    [Fact]
    public async Task SpendPointsAsync_ShouldCreateSpentTransaction()
    {
        var user = await SeedUserAsync();
        await _service.EarnPointsAsync(user.Id, 1, 100m);

        await _service.SpendPointsAsync(user.Id, 30, orderId: 2);

        var transactions = await _service.GetTransactionHistoryAsync(user.Id);
        var spentTx = transactions.First(t => t.Type == "Spent");
        spentTx.Points.Should().Be(-30);
    }

    [Fact]
    public async Task SpendPointsAsync_WithoutOrderId_ShouldWork()
    {
        var user = await SeedUserAsync();
        await _service.EarnPointsAsync(user.Id, 1, 100m);

        var result = await _service.SpendPointsAsync(user.Id, 10);
        result.SpentPoints.Should().Be(10);
    }

    #endregion

    #region RefundPointsAsync

    [Fact]
    public async Task RefundPointsAsync_ShouldRestorePoints()
    {
        var user = await SeedUserAsync();
        await _service.EarnPointsAsync(user.Id, 1, 100m);
        await _service.SpendPointsAsync(user.Id, 50, orderId: 2);

        var result = await _service.RefundPointsAsync(user.Id, 50, orderId: 2);

        result.AvailablePoints.Should().Be(100);
        result.SpentPoints.Should().Be(0);
    }

    [Fact]
    public async Task RefundPointsAsync_ShouldCreateRefundedTransaction()
    {
        var user = await SeedUserAsync();
        await _service.EarnPointsAsync(user.Id, 1, 100m);
        await _service.SpendPointsAsync(user.Id, 50, orderId: 2);

        await _service.RefundPointsAsync(user.Id, 50, orderId: 2);

        var transactions = await _service.GetTransactionHistoryAsync(user.Id);
        var refundTx = transactions.First(t => t.Type == "Refunded");
        refundTx.Points.Should().Be(50);
    }

    #endregion

    #region Tier Management

    [Fact]
    public async Task EarnPoints_ShouldUpgradeToSilverAt5000()
    {
        var user = await SeedUserAsync();

        await _service.EarnPointsAsync(user.Id, 1, 5000m);

        var result = await _service.GetUserPointsAsync(user.Id);
        result.Tier.Should().Be("Silver");
    }

    [Fact]
    public async Task EarnPoints_ShouldUpgradeToGoldAt20000()
    {
        var user = await SeedUserAsync();

        await _service.EarnPointsAsync(user.Id, 1, 5000m);
        // Silver multiplier (1.25): 5000 * 1.25 = 6250 points total now
        // Need to reach 20000 total
        await _service.EarnPointsAsync(user.Id, 2, 12000m);

        var result = await _service.GetUserPointsAsync(user.Id);
        result.Tier.Should().Be("Gold");
    }

    [Fact]
    public async Task EarnPoints_ShouldStayBronzeBelowThreshold()
    {
        var user = await SeedUserAsync();

        await _service.EarnPointsAsync(user.Id, 1, 100m);

        var result = await _service.GetUserPointsAsync(user.Id);
        result.Tier.Should().Be("Bronze");
    }

    [Fact]
    public async Task PointsToNextTier_ForBronze_ShouldShow5000()
    {
        var user = await SeedUserAsync();

        var result = await _service.GetUserPointsAsync(user.Id);
        result.PointsToNextTier.Should().Be(5000);
    }

    #endregion

    #region CalculatePointValueAsync

    [Fact]
    public async Task CalculatePointValueAsync_ShouldReturn1PercentPerPoint()
    {
        var value = await _service.CalculatePointValueAsync(100);
        value.Should().Be(1.00m); // 100 * 0.01 = 1 TL
    }

    [Fact]
    public async Task CalculatePointValueAsync_ForZeroPoints_ShouldReturnZero()
    {
        var value = await _service.CalculatePointValueAsync(0);
        value.Should().Be(0m);
    }

    [Fact]
    public async Task CalculatePointValueAsync_For1000Points_ShouldReturn10TL()
    {
        var value = await _service.CalculatePointValueAsync(1000);
        value.Should().Be(10.00m);
    }

    #endregion

    #region CalculateEarnablePointsAsync

    [Fact]
    public async Task CalculateEarnablePointsAsync_BronzeTier_ShouldReturn1xMultiplier()
    {
        var points = await _service.CalculateEarnablePointsAsync(100m, LoyaltyTier.Bronze);
        points.Should().Be(100);
    }

    [Fact]
    public async Task CalculateEarnablePointsAsync_SilverTier_ShouldReturn125xMultiplier()
    {
        var points = await _service.CalculateEarnablePointsAsync(100m, LoyaltyTier.Silver);
        points.Should().Be(125);
    }

    [Fact]
    public async Task CalculateEarnablePointsAsync_GoldTier_ShouldReturn15xMultiplier()
    {
        var points = await _service.CalculateEarnablePointsAsync(100m, LoyaltyTier.Gold);
        points.Should().Be(150);
    }

    [Fact]
    public async Task CalculateEarnablePointsAsync_PlatinumTier_ShouldReturn2xMultiplier()
    {
        var points = await _service.CalculateEarnablePointsAsync(100m, LoyaltyTier.Platinum);
        points.Should().Be(200);
    }

    #endregion

    #region AdminAdjustPointsAsync

    [Fact]
    public async Task AdminAdjustPointsAsync_PositiveAdjustment_ShouldAddPoints()
    {
        var user = await SeedUserAsync();

        var result = await _service.AdminAdjustPointsAsync(user.Id, 500, "Bonus points");

        result.AvailablePoints.Should().Be(500);
        result.TotalPoints.Should().Be(500);
    }

    [Fact]
    public async Task AdminAdjustPointsAsync_NegativeAdjustment_ShouldDeductPoints()
    {
        var user = await SeedUserAsync();
        await _service.EarnPointsAsync(user.Id, 1, 100m);

        var result = await _service.AdminAdjustPointsAsync(user.Id, -50, "Admin deduction");

        result.AvailablePoints.Should().Be(50);
    }

    [Fact]
    public async Task AdminAdjustPointsAsync_ShouldCreateAdminAdjustmentTransaction()
    {
        var user = await SeedUserAsync();

        await _service.AdminAdjustPointsAsync(user.Id, 100, "Test adjustment");

        var transactions = await _service.GetTransactionHistoryAsync(user.Id);
        var adjustTx = transactions.First(t => t.Type == "AdminAdjustment");
        adjustTx.Points.Should().Be(100);
        adjustTx.Description.Should().Be("Test adjustment");
    }

    #endregion

    #region GetTransactionHistoryAsync

    [Fact]
    public async Task GetTransactionHistoryAsync_ShouldReturnPagedResults()
    {
        var user = await SeedUserAsync();

        for (int i = 1; i <= 25; i++)
        {
            await _service.EarnPointsAsync(user.Id, i, 10m);
        }

        var page1 = await _service.GetTransactionHistoryAsync(user.Id, page: 1, pageSize: 10);
        var page2 = await _service.GetTransactionHistoryAsync(user.Id, page: 2, pageSize: 10);
        var page3 = await _service.GetTransactionHistoryAsync(user.Id, page: 3, pageSize: 10);

        page1.Should().HaveCount(10);
        page2.Should().HaveCount(10);
        page3.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetTransactionHistoryAsync_ShouldOrderByCreatedAtDescending()
    {
        var user = await SeedUserAsync();

        await _service.EarnPointsAsync(user.Id, 1, 100m);
        await _service.EarnPointsAsync(user.Id, 2, 200m);

        var transactions = await _service.GetTransactionHistoryAsync(user.Id);

        transactions.Should().HaveCount(2);
        transactions[0].CreatedAt.Should().BeOnOrAfter(transactions[1].CreatedAt);
    }

    #endregion

    #region GetTopUsersAsync

    [Fact]
    public async Task GetTopUsersAsync_ShouldReturnOrderedByTotalPoints()
    {
        var user1 = await SeedUserAsync("User 1");
        var user2 = await SeedUserAsync("User 2");
        var user3 = await SeedUserAsync("User 3");

        await _service.EarnPointsAsync(user1.Id, 1, 100m);
        await _service.EarnPointsAsync(user2.Id, 2, 300m);
        await _service.EarnPointsAsync(user3.Id, 3, 200m);

        var result = await _service.GetTopUsersAsync(3);

        result.Should().HaveCount(3);
        result[0].TotalPoints.Should().BeGreaterThanOrEqualTo(result[1].TotalPoints);
        result[1].TotalPoints.Should().BeGreaterThanOrEqualTo(result[2].TotalPoints);
    }

    [Fact]
    public async Task GetTopUsersAsync_ShouldRespectCountLimit()
    {
        for (int i = 0; i < 5; i++)
        {
            var user = await SeedUserAsync($"User{i}");
            await _service.EarnPointsAsync(user.Id, i + 1, 100m);
        }

        var result = await _service.GetTopUsersAsync(3);
        result.Should().HaveCount(3);
    }

    #endregion
}
