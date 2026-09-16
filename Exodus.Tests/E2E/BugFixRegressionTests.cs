using System.Net;
using System.Net.Http.Json;
using Exodus.Data;
using Exodus.Models.Dto;
using Exodus.Models.Entities;
using Exodus.Models.Enums;
using Exodus.Services.Campaigns;
using Exodus.Services.Common;
using Exodus.Services.Files;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Exodus.Tests.E2E;

/// <summary>
/// Regression coverage for the bug fixes: validation, authorization, discount integrity,
/// session invalidation and path containment.
/// </summary>
public class BugFixRegressionTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public BugFixRegressionTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ---------------------------------------------------------------- loyalty

    [Theory]
    [InlineData(0, "zero")]
    [InlineData(-25, "negative")]
    public async Task SpendPoints_WithNonPositiveAmount_ReturnsBadRequest(int points, string suffix)
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsCustomerAsync(client, "loyaltyreg" + suffix);

        var response = await client.PostAsJsonAsync("/api/loyalty/spend",
            new { Points = points }, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ------------------------------------------------------------ profile/auth

    [Fact]
    public async Task ChangePassword_RevokesExistingRefreshTokens()
    {
        var client = _factory.CreateClient();
        var auth = await TestHelper.RegisterAndLoginAsCustomerAsync(client, "pwdrevoke");
        var login = await TestHelper.LoginAsync(client, "customerpwdrevoke@example.com", "Customer123!@#");
        TestHelper.SetAuthToken(client, login.Token!);
        login.RefreshToken.Should().NotBeNullOrEmpty();

        var changeResponse = await client.PostAsJsonAsync("/api/profile/change-password", new ChangePasswordDto
        {
            CurrentPassword = "Customer123!@#",
            NewPassword = "NewCustomer123!@#",
            ConfirmPassword = "NewCustomer123!@#"
        }, TestHelper.JsonOptions);
        changeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var refreshResponse = await client.PostAsJsonAsync("/api/auth/refresh",
            new { RefreshToken = login.RefreshToken }, TestHelper.JsonOptions);

        refreshResponse.StatusCode.Should().NotBe(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        (await db.RefreshTokens.Where(t => t.UserId == auth.UserId).ToListAsync())
            .Should().OnlyContain(t => t.IsRevoked);
    }

    [Fact]
    public async Task GetStats_CountsWishlistItems()
    {
        var client = _factory.CreateClient();
        var auth = await TestHelper.RegisterAndLoginAsCustomerAsync(client, "wishstats");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Set<Wishlist>().Add(new Wishlist
            {
                UserId = auth.UserId,
                Items = new List<WishlistItem>
                {
                    new() { ProductId = 1 },
                    new() { ProductId = 2 }
                }
            });
            await db.SaveChangesAsync();
        }

        var response = await client.GetAsync("/api/profile/stats");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stats = await response.Content.ReadFromJsonAsync<UserStatsDto>(TestHelper.JsonOptions);
        stats!.WishlistCount.Should().Be(2);
    }

    // ---------------------------------------------------------------- refunds

    [Theory]
    [InlineData(-5, "negative")]
    [InlineData(0, "zero")]
    [InlineData(100_000, "toohigh")]
    public async Task RequestRefund_WithInvalidAmount_ReturnsBadRequest(int amount, string suffix)
    {
        var client = _factory.CreateClient();
        var auth = await TestHelper.RegisterAndLoginAsCustomerAsync(client, "refund" + suffix);
        var orderId = await SeedDeliveredOrderAsync(auth.UserId, total: 500m);

        var response = await client.PostAsJsonAsync($"/api/order/{orderId}/refund", new
        {
            Reason = "Hatalı ürün",
            Amount = amount
        }, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RequestRefund_WithForeignSellerOrder_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var auth = await TestHelper.RegisterAndLoginAsCustomerAsync(client, "refundforeign");
        var orderId = await SeedDeliveredOrderAsync(auth.UserId, total: 500m);
        var otherOrderId = await SeedDeliveredOrderAsync(auth.UserId, total: 500m);

        int foreignSellerOrderId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            foreignSellerOrderId = await db.SellerOrders
                .Where(so => so.OrderId == otherOrderId)
                .Select(so => so.Id)
                .FirstAsync();
        }

        var response = await client.PostAsJsonAsync($"/api/order/{orderId}/refund", new
        {
            Reason = "Hatalı ürün",
            SellerOrderId = foreignSellerOrderId
        }, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // -------------------------------------------------------------- campaigns

    [Fact]
    public async Task ApplyToOrder_PersistsDiscount_AndRejectsDuplicateOrForeignUse()
    {
        var client = _factory.CreateClient();
        var buyer = await TestHelper.RegisterAndLoginAsCustomerAsync(client, "campapply");
        var orderId = await SeedDeliveredOrderAsync(buyer.UserId, total: 1000m);
        var campaignId = await SeedPercentageCampaignAsync(10);

        using var scope = _factory.Services.CreateScope();
        var campaigns = scope.ServiceProvider.GetRequiredService<ICampaignService>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var result = await campaigns.ApplyToOrderAsync(buyer.UserId, orderId, campaignId);

        result.DiscountAmount.Should().Be(100m);
        var usage = await db.CampaignUsages.SingleAsync(u => u.OrderId == orderId);
        usage.DiscountApplied.Should().Be(100m);
        var order = await db.Orders.AsNoTracking().FirstAsync(o => o.Id == orderId);
        order.DiscountAmount.Should().Be(100m);
        order.TotalAmount.Should().Be(900m);

        await FluentActions
            .Awaiting(() => campaigns.ApplyToOrderAsync(buyer.UserId, orderId, campaignId))
            .Should().ThrowAsync<BadRequestException>();

        await FluentActions
            .Awaiting(() => campaigns.ApplyToOrderAsync(buyer.UserId + 9999, orderId, campaignId))
            .Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task ValidateCoupon_IsCaseInsensitive()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsAdminAsync(client, "coupon");

        var createResponse = await client.PostAsJsonAsync("/api/admin/campaigns", new
        {
            Name = "Coupon Case Campaign",
            Type = CampaignType.PercentageDiscount,
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(30),
            DiscountPercentage = 10m,
            CouponCode = "mixedCase10",
            RequiresCouponCode = true,
            Scope = CampaignScope.AllProducts
        }, TestHelper.JsonOptions);
        createResponse.IsSuccessStatusCode.Should().BeTrue();

        await TestHelper.RegisterAndLoginAsCustomerAsync(client, "couponuser");

        var lower = await client.GetAsync("/api/campaign/validate-coupon?code=mixedcase10");
        var upper = await client.GetAsync("/api/campaign/validate-coupon?code=MIXEDCASE10");

        lower.StatusCode.Should().Be(HttpStatusCode.OK);
        upper.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ------------------------------------------------------------------ files

    [Theory]
    [InlineData("../appsettings.json")]
    [InlineData("uploads/../../appsettings.json")]
    public void GetFullPath_WithTraversalPath_Throws(string relativePath)
    {
        using var scope = _factory.Services.CreateScope();
        var files = scope.ServiceProvider.GetRequiredService<IFileService>();

        var act = () => files.GetFullPath(relativePath);

        act.Should().Throw<BadRequestException>();
    }

    [Fact]
    public void GetFullPath_WithNormalPath_StaysInsideWebRoot()
    {
        using var scope = _factory.Services.CreateScope();
        var files = scope.ServiceProvider.GetRequiredService<IFileService>();

        var path = files.GetFullPath("/uploads/general/image.png");

        path.Should().EndWith(Path.Combine("uploads", "general", "image.png"));
    }

    // ----------------------------------------------------------------- seeder

    [Fact]
    public async Task Seeder_StoresBcryptHashedPasswords()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase("SeederTest_" + Guid.NewGuid().ToString("N"))
            .Options;

        await using var db = new ApplicationDbContext(options);
        await DbSeeder.SeedAsync(db);

        var users = await db.Users.ToListAsync();
        users.Should().NotBeEmpty();
        users.Should().OnlyContain(u => u.Password.StartsWith("$2"));
        BCrypt.Net.BCrypt.Verify(DbSeeder.DemoPassword, users[0].Password).Should().BeTrue();
    }

    // ---------------------------------------------------------------- helpers

    private async Task<int> SeedDeliveredOrderAsync(int buyerId, decimal total)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var order = new Order
        {
            OrderNumber = "TEST-" + Guid.NewGuid().ToString("N")[..12],
            BuyerId = buyerId,
            Status = OrderStatus.Delivered,
            SubTotal = total,
            TotalAmount = total,
            DeliveredAt = DateTime.UtcNow,
            SellerOrders = new List<SellerOrder>
            {
                new()
                {
                    SellerId = buyerId,
                    Status = SellerOrderStatus.Delivered,
                    SubTotal = total,
                    Items = new List<SellerOrderItem>
                    {
                        new()
                        {
                            ListingId = 1,
                            ProductId = 1,
                            ProductName = "Test Product",
                            UnitPrice = total,
                            Quantity = 1,
                            LineTotal = total
                        }
                    }
                }
            }
        };

        db.Orders.Add(order);
        await db.SaveChangesAsync();
        return order.Id;
    }

    private async Task<int> SeedPercentageCampaignAsync(decimal percentage)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var campaign = new Campaign
        {
            Name = "Regression Campaign",
            Type = CampaignType.PercentageDiscount,
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(30),
            IsActive = true,
            DiscountPercentage = percentage,
            Scope = CampaignScope.AllProducts
        };

        db.Campaigns.Add(campaign);
        await db.SaveChangesAsync();
        return campaign.Id;
    }
}
