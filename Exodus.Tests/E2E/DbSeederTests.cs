using Exodus.Data;
using Exodus.Models.Entities;
using Exodus.Models.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Exodus.Tests.E2E;

public class DbSeederTests
{
    private static async Task<ApplicationDbContext> SeededDbAsync()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase("DbSeederTests_" + Guid.NewGuid().ToString("N"))
            .Options;

        var db = new ApplicationDbContext(options);
        await DbSeeder.SeedAsync(db);
        return db;
    }

    [Fact]
    public async Task Seed_WithDevAdmin_CreatesOneAdmin()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase("DbSeederTests_" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new ApplicationDbContext(options);

        await DbSeeder.SeedAsync(db, includeDevAdmin: true);
        await DbSeeder.SeedAsync(db, includeDevAdmin: true);

        var admins = await db.Users.Where(u => u.Role == UserRole.Admin).ToListAsync();
        admins.Should().ContainSingle().Which.Email.Should().Be("admin@test.com");
    }

    [Fact]
    public async Task Seed_WithoutDevAdmin_CreatesNoAdmin()
    {
        await using var db = await SeededDbAsync();

        (await db.Users.CountAsync(u => u.Role == UserRole.Admin)).Should().Be(0);
    }

    [Fact]
    public async Task Seed_ListingsBelongOnlyToSellers()
    {
        await using var db = await SeededDbAsync();

        var listings = await db.Listings.Include(l => l.Seller).ToListAsync();

        listings.Should().NotBeEmpty();
        listings.Should().OnlyContain(l => l.Seller.Role == UserRole.Seller);
        listings.Select(l => l.Seller.Email).Distinct()
            .Should().BeEquivalentTo(new[] { "zeynep@test.com", "mehmet@test.com" });
    }

    [Fact]
    public async Task Seed_EveryCustomerHasDefaultAddress()
    {
        await using var db = await SeededDbAsync();

        var customers = await db.Users.Where(u => u.Role == UserRole.Customer).ToListAsync();
        customers.Should().NotBeEmpty();

        foreach (var c in customers)
        {
            var defaults = await db.Addresses.Where(a => a.UserId == c.Id && a.IsDefault).ToListAsync();
            defaults.Should().ContainSingle($"{c.Email} needs exactly one default address");
            defaults[0].Type.Should().Be(AddressType.Both);
        }
    }

    [Fact]
    public async Task Seed_ProductsHaveCategories()
    {
        await using var db = await SeededDbAsync();

        var products = await db.Products.Include(p => p.Category).ToListAsync();

        products.Count.Should().BeGreaterThanOrEqualTo(15);
        products.Should().OnlyContain(p => p.CategoryId != null);
        products.Should().OnlyContain(p => p.Brand != null && p.Brand != "");

        var categories = await db.Categories.ToListAsync();
        categories.Select(c => c.Slug).Should().BeEquivalentTo(
            new[] { "elektronik", "ev-yasam", "spor", "kitap", "moda" });
        foreach (var c in categories)
            products.Count(p => p.CategoryId == c.Id).Should().BeGreaterThanOrEqualTo(3, c.Slug);
    }

    [Fact]
    public async Task Seed_HasKulaklikProductListedByZeynep()
    {
        await using var db = await SeededDbAsync();

        var listing = await db.Listings
            .Include(l => l.Product).Include(l => l.Seller)
            .FirstOrDefaultAsync(l => l.Product.ProductName.Contains("Kulaklık")
                                      && l.Seller.Email == "zeynep@test.com");

        listing.Should().NotBeNull();
    }

    [Fact]
    public async Task Seed_HasCompetingListings()
    {
        await using var db = await SeededDbAsync();

        var competing = await db.Listings
            .GroupBy(l => l.ProductId)
            .Where(g => g.Select(x => x.SellerId).Distinct().Count() >= 2)
            .Select(g => new { g.Key, Prices = g.Select(x => x.Price).Distinct().Count() })
            .ToListAsync();

        competing.Count.Should().BeGreaterThanOrEqualTo(4);
        competing.Should().OnlyContain(c => c.Prices >= 2);
    }

    [Fact]
    public async Task Seed_HasLowAndOutOfStockListings_WithConsistentStatus()
    {
        await using var db = await SeededDbAsync();

        var listings = await db.Listings.ToListAsync();

        listings.Count(l => l.StockStatus == StockStatus.LowStock).Should().BeGreaterThanOrEqualTo(2);
        listings.Count(l => l.StockStatus == StockStatus.OutOfStock).Should().BeGreaterThanOrEqualTo(1);

        foreach (var l in listings)
        {
            var expected = l.StockQuantity == 0 ? StockStatus.OutOfStock
                : l.StockQuantity <= l.LowStockThreshold ? StockStatus.LowStock
                : StockStatus.InStock;
            l.StockStatus.Should().Be(expected, $"listing {l.Id} stock {l.StockQuantity}");
        }
    }

    [Fact]
    public async Task Seed_HasDemoOrdersForBothSellers()
    {
        await using var db = await SeededDbAsync();

        foreach (var email in new[] { "zeynep@test.com", "mehmet@test.com" })
        {
            var count = await db.SellerOrders.CountAsync(so => so.Seller.Email == email);
            count.Should().BeGreaterThanOrEqualTo(3, email);
        }
    }

    [Fact]
    public async Task Seed_OrdersAreCoherent()
    {
        await using var db = await SeededDbAsync();

        var orders = await db.Orders
            .Include(o => o.Buyer)
            .Include(o => o.SellerOrders).ThenInclude(so => so.Items)
            .Include(o => o.SellerOrders).ThenInclude(so => so.Shipment)
            .OrderBy(o => o.CustomerNote)
            .ToListAsync();

        orders.Should().HaveCount(5);
        orders.Select(o => o.CustomerNote).Should().BeEquivalentTo(
            new[] { "SEED-1", "SEED-2", "SEED-3", "SEED-4", "SEED-5" });
        orders.Should().OnlyContain(o => o.Buyer.Email == "ahmet@test.com" || o.Buyer.Email == "elif@test.com");
        orders.Count(o => o.Status == OrderStatus.Delivered).Should().Be(2);
        orders.Count(o => o.Status == OrderStatus.Shipped).Should().Be(1);
        orders.Count(o => o.Status == OrderStatus.Confirmed).Should().Be(1);
        orders.Count(o => o.Status == OrderStatus.Pending).Should().Be(1);

        var now = DateTime.UtcNow;
        foreach (var o in orders)
        {
            o.SellerOrders.Should().HaveCount(2);
            o.SellerOrders.Select(s => s.SellerId).Distinct().Should().HaveCount(2);
            o.CreatedAt.Should().BeAfter(now.AddDays(-10)).And.BeBefore(now);
            o.ShippingAddressId.Should().NotBeNull();
            o.ShippingAddressSnapshot.Should().NotBeNullOrWhiteSpace();

            foreach (var so in o.SellerOrders)
            {
                so.Items.Should().NotBeEmpty();
                so.SubTotal.Should().Be(so.Items.Sum(i => i.LineTotal));
                so.Items.Should().OnlyContain(i => i.LineTotal == i.UnitPrice * i.Quantity);
            }
            o.TotalAmount.Should().Be(o.SellerOrders.Sum(s => s.SubTotal));

            switch (o.Status)
            {
                case OrderStatus.Delivered:
                    o.DeliveredAt.Should().NotBeNull();
                    o.SellerOrders.Should().OnlyContain(s => s.Status == SellerOrderStatus.Delivered
                        && s.Shipment != null && s.Shipment.Status == ShipmentStatus.Delivered);
                    break;
                case OrderStatus.Shipped:
                    o.ShippedAt.Should().NotBeNull();
                    o.SellerOrders.Should().OnlyContain(s => s.Status == SellerOrderStatus.Shipped
                        && s.Shipment != null && s.Shipment.Status == ShipmentStatus.Shipped
                        && !string.IsNullOrEmpty(s.Shipment.Carrier)
                        && !string.IsNullOrEmpty(s.Shipment.TrackingNumber));
                    break;
                case OrderStatus.Confirmed:
                    o.SellerOrders.Should().OnlyContain(s => s.Status == SellerOrderStatus.Confirmed && s.Shipment == null);
                    break;
                case OrderStatus.Pending:
                    o.SellerOrders.Should().OnlyContain(s => s.Status == SellerOrderStatus.Placed && s.Shipment == null);
                    break;
            }
        }
    }

    [Fact]
    public async Task Seed_CartsStartEmpty()
    {
        await using var db = await SeededDbAsync();

        (await db.Carts.CountAsync()).Should().BeGreaterThan(0);
        (await db.CartItems.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Seed_IsIdempotent()
    {
        await using var db = await SeededDbAsync();

        async Task<int[]> CountsAsync() => new[]
        {
            await db.Users.CountAsync(), await db.Products.CountAsync(), await db.Categories.CountAsync(),
            await db.Listings.CountAsync(), await db.Addresses.CountAsync(), await db.Orders.CountAsync(),
            await db.SellerOrders.CountAsync(), await db.SellerOrderItems.CountAsync(),
            await db.Shipments.CountAsync(), await db.Carts.CountAsync()
        };

        var before = await CountsAsync();
        await DbSeeder.SeedAsync(db);
        var after = await CountsAsync();

        after.Should().Equal(before);
    }

    [Fact]
    public async Task Seed_Again_WithDuplicateNamesEmailsAndSlugs_DoesNotThrow()
    {
        await using var db = await SeededDbAsync();
        var listingCount = await db.Listings.IgnoreQueryFilters().CountAsync();
        var orderCount = await db.Orders.IgnoreQueryFilters().CountAsync();
        var elektronik = await db.Categories.SingleAsync(c => c.Slug == "elektronik");

        // ProductService allows duplicate product names.
        db.Products.Add(new Product { ProductName = "AirPods Pro 2", ProductDescription = "Duplicate name", CategoryId = elektronik.Id });
        // The unique slug/email indexes are filtered on IsDeleted = 0, so soft-deleted rows may repeat a seeded key.
        db.Categories.Add(new Category { Name = "Eski Elektronik", Slug = "elektronik", IsDeleted = true, DeletedDate = DateTime.UtcNow });
        db.Users.Add(new Users { Name = "Old Ahmet", Email = "ahmet@test.com", Password = "x", Username = "oldahmet", Role = UserRole.Customer, IsDeleted = true, DeletedDate = DateTime.UtcNow });
        db.Users.Add(new Users { Name = "Old Zeynep", Email = "zeynep@test.com", Password = "x", Username = "oldzeynep", Role = UserRole.Seller, IsDeleted = true, DeletedDate = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var reseed = () => DbSeeder.SeedAsync(db);

        await reseed.Should().NotThrowAsync();
        (await db.Listings.IgnoreQueryFilters().CountAsync()).Should().Be(listingCount);
        (await db.Orders.IgnoreQueryFilters().CountAsync()).Should().Be(orderCount);
        var seededAirPods = await db.Products.IgnoreQueryFilters().Include(p => p.Barcodes)
            .Where(p => p.ProductName == "AirPods Pro 2").OrderBy(p => p.Id).ToListAsync();
        seededAirPods[0].Barcodes.Select(b => b.Barcode).Should().Contain("APPLE-APP2");
        seededAirPods[1].Barcodes.Should().BeEmpty("re-seeding must not copy seeded barcodes onto the duplicate");
    }
}
