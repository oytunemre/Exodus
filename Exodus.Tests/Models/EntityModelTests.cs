using Exodus.Models.Entities;
using Exodus.Models.Enums;
using FluentAssertions;
using Xunit;

namespace Exodus.Tests.Models;

public class EntityModelTests
{
    #region BaseEntity

    [Fact]
    public void BaseEntity_DefaultValues_ShouldBeCorrect()
    {
        var user = new Users
        {
            Name = "Test",
            Email = "test@test.com",
            Password = "pass",
            Username = "test"
        };

        user.IsDeleted.Should().BeFalse();
        user.DeletedDate.Should().BeNull();
        user.Id.Should().Be(0);
    }

    #endregion

    #region Users

    [Fact]
    public void Users_DefaultRole_ShouldBeCustomer()
    {
        var user = new Users
        {
            Name = "Test",
            Email = "test@test.com",
            Password = "pass",
            Username = "test"
        };

        user.Role.Should().Be(UserRole.Customer);
    }

    [Fact]
    public void Users_DefaultValues_ShouldBeCorrect()
    {
        var user = new Users
        {
            Name = "Test",
            Email = "test@test.com",
            Password = "pass",
            Username = "test"
        };

        user.EmailVerified.Should().BeFalse();
        user.FailedLoginAttempts.Should().Be(0);
        user.LockoutEndTime.Should().BeNull();
        user.TwoFactorEnabled.Should().BeFalse();
        user.LastLoginAt.Should().BeNull();
    }

    [Fact]
    public void Users_ShouldSetAllProperties()
    {
        var user = new Users
        {
            Name = "John Doe",
            Email = "john@example.com",
            Password = "hashedpass",
            Username = "johndoe",
            Role = UserRole.Seller,
            Phone = "+905551234567",
            AvatarUrl = "/images/avatar.jpg"
        };

        user.Name.Should().Be("John Doe");
        user.Email.Should().Be("john@example.com");
        user.Username.Should().Be("johndoe");
        user.Role.Should().Be(UserRole.Seller);
        user.Phone.Should().Be("+905551234567");
    }

    #endregion

    #region Product

    [Fact]
    public void Product_ShouldInitializeBarcodesCollection()
    {
        var product = new Product
        {
            ProductName = "Test",
            ProductDescription = "Desc"
        };

        product.Barcodes.Should().NotBeNull();
        product.Barcodes.Should().BeEmpty();
    }

    [Fact]
    public void Product_ShouldInitializeImagesCollection()
    {
        var product = new Product
        {
            ProductName = "Test",
            ProductDescription = "Desc"
        };

        product.Images.Should().NotBeNull();
        product.Images.Should().BeEmpty();
    }

    [Fact]
    public void Product_ShouldAcceptCategoryId()
    {
        var product = new Product
        {
            ProductName = "Test",
            ProductDescription = "Desc",
            CategoryId = 5
        };

        product.CategoryId.Should().Be(5);
    }

    #endregion

    #region Listing

    [Fact]
    public void Listing_DefaultValues_ShouldBeCorrect()
    {
        var listing = new Listing();

        listing.StockQuantity.Should().Be(0);
        listing.LowStockThreshold.Should().Be(5);
        listing.TrackInventory.Should().BeTrue();
        listing.StockStatus.Should().Be(StockStatus.InStock);
        listing.Condition.Should().Be(ListingCondition.New);
        listing.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Listing_IsLowStock_WhenStockBelowThreshold_ShouldBeTrue()
    {
        var listing = new Listing
        {
            StockQuantity = 3,
            LowStockThreshold = 5,
            TrackInventory = true
        };

        listing.IsLowStock.Should().BeTrue();
    }

    [Fact]
    public void Listing_IsLowStock_WhenStockAboveThreshold_ShouldBeFalse()
    {
        var listing = new Listing
        {
            StockQuantity = 10,
            LowStockThreshold = 5,
            TrackInventory = true
        };

        listing.IsLowStock.Should().BeFalse();
    }

    [Fact]
    public void Listing_IsLowStock_WhenStockIsZero_ShouldBeFalse()
    {
        var listing = new Listing
        {
            StockQuantity = 0,
            LowStockThreshold = 5,
            TrackInventory = true
        };

        listing.IsLowStock.Should().BeFalse();
    }

    [Fact]
    public void Listing_IsLowStock_WhenTrackInventoryFalse_ShouldBeFalse()
    {
        var listing = new Listing
        {
            StockQuantity = 2,
            LowStockThreshold = 5,
            TrackInventory = false
        };

        listing.IsLowStock.Should().BeFalse();
    }

    [Fact]
    public void Listing_IsLowStock_WhenStockEqualsThreshold_ShouldBeTrue()
    {
        var listing = new Listing
        {
            StockQuantity = 5,
            LowStockThreshold = 5,
            TrackInventory = true
        };

        listing.IsLowStock.Should().BeTrue();
    }

    #endregion

    #region Cart

    [Fact]
    public void Cart_ShouldInitializeItemsCollection()
    {
        var cart = new Cart();
        cart.Items.Should().NotBeNull();
        cart.Items.Should().BeEmpty();
    }

    #endregion

    #region Order

    [Fact]
    public void Order_DefaultValues_ShouldBeCorrect()
    {
        var order = new Order
        {
            OrderNumber = "ORD-001"
        };

        order.Status.Should().Be(OrderStatus.Pending);
        order.Currency.Should().Be("TRY");
        order.SellerOrders.Should().BeEmpty();
        order.OrderEvents.Should().BeEmpty();
        order.SubTotal.Should().Be(0);
        order.ShippingCost.Should().Be(0);
        order.TaxAmount.Should().Be(0);
        order.DiscountAmount.Should().Be(0);
        order.TotalAmount.Should().Be(0);
    }

    [Fact]
    public void Order_ShouldInitializeCollections()
    {
        var order = new Order { OrderNumber = "ORD-001" };
        order.SellerOrders.Should().NotBeNull();
        order.OrderEvents.Should().NotBeNull();
    }

    #endregion

    #region SellerOrder

    [Fact]
    public void SellerOrder_DefaultStatus_ShouldBePlaced()
    {
        var so = new SellerOrder();
        so.Status.Should().Be(SellerOrderStatus.Placed);
    }

    [Fact]
    public void SellerOrder_ShouldInitializeItemsCollection()
    {
        var so = new SellerOrder();
        so.Items.Should().NotBeNull();
        so.Items.Should().BeEmpty();
    }

    #endregion

    #region PaymentIntent

    [Fact]
    public void PaymentIntent_DefaultValues_ShouldBeCorrect()
    {
        var intent = new PaymentIntent();
        intent.Status.Should().Be(PaymentStatus.Created);
        intent.Currency.Should().Be("TRY");
        intent.Provider.Should().Be("MANUAL");
        intent.RefundedAmount.Should().Be(0);
        intent.Requires3DSecure.Should().BeFalse();
    }

    #endregion

    #region Category

    [Fact]
    public void Category_ShouldInitializeCollections()
    {
        var cat = new Category { Name = "Test", Slug = "test" };
        cat.SubCategories.Should().NotBeNull();
        cat.Products.Should().NotBeNull();
    }

    [Fact]
    public void Category_DefaultValues_ShouldBeCorrect()
    {
        var cat = new Category { Name = "Test", Slug = "test" };
        cat.IsActive.Should().BeTrue();
        cat.DisplayOrder.Should().Be(0);
        cat.ParentCategoryId.Should().BeNull();
    }

    #endregion

    #region LoyaltyPoint

    [Fact]
    public void LoyaltyPoint_DefaultValues_ShouldBeCorrect()
    {
        var loyalty = new LoyaltyPoint();
        loyalty.TotalPoints.Should().Be(0);
        loyalty.AvailablePoints.Should().Be(0);
        loyalty.SpentPoints.Should().Be(0);
        loyalty.PendingPoints.Should().Be(0);
        loyalty.Tier.Should().Be(LoyaltyTier.Bronze);
    }

    #endregion

    #region RefreshToken

    [Fact]
    public void RefreshToken_DefaultValues_ShouldBeCorrect()
    {
        var token = new RefreshToken { Token = "test-token" };
        token.IsRevoked.Should().BeFalse();
        token.RevokedAt.Should().BeNull();
    }

    #endregion

    #region Enum Values

    [Theory]
    [InlineData(UserRole.Customer, 0)]
    [InlineData(UserRole.Seller, 1)]
    [InlineData(UserRole.Admin, 2)]
    public void UserRole_ShouldHaveCorrectValues(UserRole role, int expected)
    {
        ((int)role).Should().Be(expected);
    }

    [Theory]
    [InlineData(OrderStatus.Pending, 0)]
    [InlineData(OrderStatus.Processing, 1)]
    [InlineData(OrderStatus.Confirmed, 2)]
    [InlineData(OrderStatus.Shipped, 3)]
    [InlineData(OrderStatus.Delivered, 4)]
    [InlineData(OrderStatus.Completed, 5)]
    [InlineData(OrderStatus.Cancelled, 6)]
    [InlineData(OrderStatus.Refunded, 7)]
    [InlineData(OrderStatus.PartialRefund, 8)]
    [InlineData(OrderStatus.Failed, 9)]
    public void OrderStatus_ShouldHaveCorrectValues(OrderStatus status, int expected)
    {
        ((int)status).Should().Be(expected);
    }

    [Theory]
    [InlineData(ListingCondition.New, 0)]
    [InlineData(ListingCondition.LikeNew, 1)]
    [InlineData(ListingCondition.Used, 2)]
    [InlineData(ListingCondition.Refurbished, 3)]
    public void ListingCondition_ShouldHaveCorrectValues(ListingCondition condition, int expected)
    {
        ((int)condition).Should().Be(expected);
    }

    [Fact]
    public void PaymentMethod_ShouldHaveAllValues()
    {
        var values = Enum.GetValues<PaymentMethod>();
        values.Should().HaveCount(7);
    }

    [Fact]
    public void PaymentStatus_ShouldHaveAllValues()
    {
        var values = Enum.GetValues<PaymentStatus>();
        values.Should().HaveCount(9);
    }

    #endregion
}
