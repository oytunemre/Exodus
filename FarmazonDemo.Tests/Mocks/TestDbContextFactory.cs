using FarmazonDemo.Data;
using FarmazonDemo.Models.Entities;
using FarmazonDemo.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace FarmazonDemo.Tests.Mocks;

public static class TestDbContextFactory
{
    public static ApplicationDbContext CreateInMemoryContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: databaseName)
            .Options;

        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public static void SeedTestData(ApplicationDbContext context)
    {
        // Add test users
        var user1 = new Users
        {
            Id = 1,
            Name = "Test User",
            Email = "test@example.com",
            Username = "testuser",
            Password = BCrypt.Net.BCrypt.HashPassword("Test123!"),
            Role = UserRole.Customer,
            EmailVerified = true
        };

        var seller = new Users
        {
            Id = 2,
            Name = "Test Seller",
            Email = "seller@example.com",
            Username = "testseller",
            Password = BCrypt.Net.BCrypt.HashPassword("Test123!"),
            Role = UserRole.Seller,
            EmailVerified = true
        };

        var admin = new Users
        {
            Id = 3,
            Name = "Test Admin",
            Email = "admin@example.com",
            Username = "testadmin",
            Password = BCrypt.Net.BCrypt.HashPassword("Test123!"),
            Role = UserRole.Admin,
            EmailVerified = true
        };

        context.Users.AddRange(user1, seller, admin);

        // Add test category
        var category = new Category
        {
            Id = 1,
            Name = "Test Category",
            Slug = "test-category",
            IsActive = true
        };
        context.Categories.Add(category);

        // Add test brand
        var brand = new Brand
        {
            Id = 1,
            Name = "Test Brand",
            Slug = "test-brand",
            IsActive = true
        };
        context.Brands.Add(brand);

        // Add test product
        var product = new Product
        {
            Id = 1,
            ProductName = "Test Product",
            Description = "Test Description",
            CategoryId = 1,
            BrandId = 1,
            IsActive = true
        };
        context.Products.Add(product);

        // Add test listing
        var listing = new Listing
        {
            Id = 1,
            ProductId = 1,
            SellerId = 2,
            Price = 100.00m,
            StockQuantity = 50,
            StockStatus = StockStatus.InStock,
            IsActive = true,
            LowStockThreshold = 5
        };
        context.Listings.Add(listing);

        // Add test cart
        var cart = new Cart
        {
            Id = 1,
            UserId = 1
        };
        context.Carts.Add(cart);

        // Add test cart item
        var cartItem = new CartItem
        {
            Id = 1,
            CartId = 1,
            ListingId = 1,
            Quantity = 2,
            UnitPrice = 100.00m
        };
        context.CartItems.Add(cartItem);

        // Add test address
        var address = new Address
        {
            Id = 1,
            UserId = 1,
            Title = "Home",
            FullName = "Test User",
            Phone = "+905551234567",
            City = "Istanbul",
            District = "Kadikoy",
            AddressLine = "Test Address Line 1",
            IsDefault = true,
            Type = AddressType.Shipping
        };
        context.Addresses.Add(address);

        context.SaveChanges();
    }

    public static void SeedOrderTestData(ApplicationDbContext context)
    {
        SeedTestData(context);

        // Add test order
        var order = new Order
        {
            Id = 1,
            OrderNumber = "ORD-20260205-TEST0001",
            BuyerId = 1,
            Status = OrderStatus.Pending,
            SubTotal = 200.00m,
            ShippingCost = 10.00m,
            TaxAmount = 0m,
            DiscountAmount = 0m,
            TotalAmount = 210.00m,
            ShippingAddressId = 1,
            ShippingAddressSnapshot = "Test User, Test Address Line 1, Kadikoy/Istanbul"
        };
        context.Orders.Add(order);

        // Add seller order
        var sellerOrder = new SellerOrder
        {
            Id = 1,
            OrderId = 1,
            SellerId = 2,
            Status = SellerOrderStatus.Placed,
            SubTotal = 200.00m
        };
        context.SellerOrders.Add(sellerOrder);

        // Add seller order item
        var sellerOrderItem = new SellerOrderItem
        {
            Id = 1,
            SellerOrderId = 1,
            ListingId = 1,
            ProductId = 1,
            ProductName = "Test Product",
            UnitPrice = 100.00m,
            Quantity = 2,
            LineTotal = 200.00m
        };
        context.SellerOrderItems.Add(sellerOrderItem);

        context.SaveChanges();
    }

    public static void SeedPaymentTestData(ApplicationDbContext context)
    {
        SeedOrderTestData(context);

        // Add test payment intent
        var paymentIntent = new PaymentIntent
        {
            Id = 1,
            OrderId = 1,
            Amount = 210.00m,
            Currency = "TRY",
            Method = PaymentMethod.CreditCard,
            Status = PaymentStatus.Created,
            Provider = "MANUAL",
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };
        context.PaymentIntents.Add(paymentIntent);

        context.SaveChanges();
    }
}
