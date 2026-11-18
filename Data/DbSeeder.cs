using Exodus.Models.Entities;
using Exodus.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace Exodus.Data;

public static class DbSeeder
{
    public const string DemoPassword = "Demo123!";

    public static async Task MigrateAsync(ApplicationDbContext db)
    {
        if (db.Database.IsRelational())
            await db.Database.MigrateAsync();
    }

    /// <summary>
    /// Lookup keys are not unique in the database (product names may repeat; the unique email/slug
    /// indexes ignore soft-deleted rows), so keep one row per key: the one with the lowest Id.
    /// </summary>
    private static Dictionary<string, T> LowestIdPerKey<T>(IEnumerable<T> rows, Func<T, string> key, Func<T, int> id) =>
        rows.GroupBy(key).ToDictionary(g => g.Key, g => g.MinBy(id)!);

    public static async Task SeedAsync(ApplicationDbContext db, bool includeDevAdmin = false)
    {
        await MigrateAsync(db);

        await SeedUsersAsync(db, includeDevAdmin);
        await SeedCategoriesAsync(db);
        await SeedProductsAsync(db);
        await SeedListingsAsync(db);
        await SeedCartsAsync(db);
        await SeedAddressesAsync(db);
        await SeedOrdersAsync(db);
    }

    // ------------------------------------------------------------------ users
    // Key: Email.
    private static async Task SeedUsersAsync(ApplicationDbContext db, bool includeDevAdmin)
    {
        var demoPasswordHash = BCrypt.Net.BCrypt.HashPassword(DemoPassword);

        var usersToSeed = new List<Users>
        {
            new Users { Name = "Ahmet Yılmaz",  Email = "ahmet@test.com",  Password = demoPasswordHash, Username = "ahmety",  Role = UserRole.Customer, EmailVerified = true },
            new Users { Name = "Zeynep Kaya",   Email = "zeynep@test.com", Password = demoPasswordHash, Username = "zeynepk", Role = UserRole.Seller,   EmailVerified = true },
            new Users { Name = "Mehmet Demir",  Email = "mehmet@test.com", Password = demoPasswordHash, Username = "mehmetd", Role = UserRole.Seller,   EmailVerified = true },
            new Users { Name = "Elif Şahin",    Email = "elif@test.com",   Password = demoPasswordHash, Username = "elifs",   Role = UserRole.Customer, EmailVerified = true },
            new Users { Name = "Can Arslan",    Email = "can@test.com",    Password = demoPasswordHash, Username = "canars",  Role = UserRole.Customer, EmailVerified = true }
        };

        // Known-password admin: Development only (caller decides).
        if (includeDevAdmin)
            usersToSeed.Add(new Users { Name = "Demo Admin", Email = "admin@test.com", Password = demoPasswordHash, Username = "demoadmin", Role = UserRole.Admin, EmailVerified = true });

        foreach (var u in usersToSeed)
        {
            var exists = await db.Users.IgnoreQueryFilters().AnyAsync(x => x.Email == u.Email);
            if (!exists) db.Users.Add(u);
        }
        await db.SaveChangesAsync();
    }

    // ------------------------------------------------------------- categories
    // Key: Slug.
    private static readonly (string Name, string Slug, int Order)[] CategorySpecs =
    {
        ("Elektronik", "elektronik", 1),
        ("Ev & Yaşam", "ev-yasam", 2),
        ("Spor", "spor", 3),
        ("Kitap", "kitap", 4),
        ("Moda", "moda", 5)
    };

    private static async Task SeedCategoriesAsync(ApplicationDbContext db)
    {
        foreach (var (name, slug, order) in CategorySpecs)
        {
            var exists = await db.Categories.IgnoreQueryFilters().AnyAsync(c => c.Slug == slug);
            if (!exists)
                db.Categories.Add(new Category { Name = name, Slug = slug, DisplayOrder = order, IsActive = true });
        }
        await db.SaveChangesAsync();
    }

    // --------------------------------------------------------------- products
    // Key: ProductName.
    private sealed record ProductSpec(string Name, string Desc, string Brand, string CategorySlug, string[] Barcodes);

    private static readonly ProductSpec[] ProductSpecs =
    {
        // Elektronik
        new("Logitech MX Master 3S", "Kablosuz mouse", "Logitech", "elektronik", new[] { "LOGI-MX3S", "LOGI-MX3S-ALT" }),
        new("AirPods Pro 2", "ANC kulaklık", "Apple", "elektronik", new[] { "APPLE-APP2", "APPLE-APP2-ALT" }),
        new("Samsung 25W Type-C", "Telefon adaptörü", "Samsung", "elektronik", new[] { "SAM-25W-TR", "SAM-25W-EU" }),
        new("Anker PowerCore 10000", "Powerbank", "Anker", "elektronik", new[] { "ANK-PC10K", "ANK-PC10K-ALT" }),
        new("HP 27\" IPS Monitor", "Full HD monitör", "HP", "elektronik", new[] { "HP-27IPS-FHD", "HP-27IPS-FHD-ALT" }),
        new("Sony WH-1000XM5 Kablosuz Kulaklık", "Gürültü engelleyici kablosuz kulak üstü kulaklık", "Sony", "elektronik", new[] { "SONY-WH1000XM5" }),
        // Ev & Yaşam
        new("Philips Airfryer XL", "6.2 litre yağsız fritöz", "Philips", "ev-yasam", new[] { "PHI-AFXL" }),
        new("Karaca Çelik Tencere Seti", "8 parça çelik tencere seti", "Karaca", "ev-yasam", new[] { "KRC-TNC8" }),
        new("Xiaomi Robot Süpürge S10", "Akıllı robot süpürge", "Xiaomi", "ev-yasam", new[] { "XIA-RS10" }),
        // Spor
        new("Decathlon Yoga Matı 8mm", "Kaymaz yoga ve pilates matı", "Decathlon", "spor", new[] { "DEC-YOGA8" }),
        new("Nike Air Zoom Pegasus Koşu Ayakkabısı", "Günlük koşu ayakkabısı", "Nike", "spor", new[] { "NIKE-PEGASUS" }),
        new("Adidas Antrenman Spor Çantası", "35 litre spor çanta", "Adidas", "spor", new[] { "ADI-BAG35" }),
        // Kitap
        new("Sapiens: Hayvanlardan Tanrılara", "Yuval Noah Harari, insanlık tarihi", "Kolektif Kitap", "kitap", new[] { "9786059550000" }),
        new("Simyacı", "Paulo Coelho, roman", "Can Yayınları", "kitap", new[] { "9789750726439" }),
        new("Suç ve Ceza", "Dostoyevski, klasik roman", "İş Bankası Kültür Yayınları", "kitap", new[] { "9786053602080" }),
        // Moda
        new("Levi's 501 Erkek Kot Pantolon", "Klasik straight fit kot", "Levi's", "moda", new[] { "LEVIS-501" }),
        new("Mavi Kadın Triko Kazak", "Yün karışımlı triko", "Mavi", "moda", new[] { "MAVI-TRK" }),
        new("Lacoste Erkek Polo Tişört", "Pamuklu polo yaka tişört", "Lacoste", "moda", new[] { "LAC-POLO" })
    };

    private static async Task SeedProductsAsync(ApplicationDbContext db)
    {
        var slugs = ProductSpecs.Select(p => p.CategorySlug).Distinct().ToList();
        var categories = LowestIdPerKey(
            await db.Categories.IgnoreQueryFilters()
                .Where(c => slugs.Contains(c.Slug))
                .Select(c => new { c.Id, c.Slug })
                .ToListAsync(),
            c => c.Slug, c => c.Id);

        foreach (var spec in ProductSpecs)
        {
            var categoryId = categories[spec.CategorySlug].Id;

            var product = await db.Products
                .IgnoreQueryFilters()
                .Include(p => p.Barcodes)
                .OrderBy(p => p.Id)
                .FirstOrDefaultAsync(p => p.ProductName == spec.Name);

            if (product is null)
            {
                db.Products.Add(new Product
                {
                    ProductName = spec.Name,
                    ProductDescription = spec.Desc,
                    Brand = spec.Brand,
                    CategoryId = categoryId,
                    Barcodes = spec.Barcodes.Select(b => new ProductBarcode { Barcode = b }).ToList()
                });
                continue;
            }

            if (product.IsDeleted)
            {
                product.IsDeleted = false;
                product.DeletedDate = null;
            }

            // Fill gaps left by older seeds.
            product.CategoryId ??= categoryId;
            if (string.IsNullOrWhiteSpace(product.Brand)) product.Brand = spec.Brand;

            var existingBarcodes = product.Barcodes.Select(x => x.Barcode).ToHashSet();
            foreach (var bc in spec.Barcodes)
            {
                if (!existingBarcodes.Contains(bc))
                    product.Barcodes.Add(new ProductBarcode { Barcode = bc });
            }
        }
        await db.SaveChangesAsync();
    }

    // --------------------------------------------------------------- listings
    // Key: (SellerId, ProductId). Only users with Role == Seller own listings.
    private sealed record ListingSpec(
        string SellerEmail, string ProductName, decimal Price, int Stock, ListingCondition Condition, string Sku, int LowStockThreshold = 5);

    private const string Zeynep = "zeynep@test.com";
    private const string Mehmet = "mehmet@test.com";

    private static readonly ListingSpec[] ListingSpecs =
    {
        // Zeynep
        new(Zeynep, "Logitech MX Master 3S", 3999.90m, 10, ListingCondition.New, "ZYN-LOGI-MX3S"),
        new(Zeynep, "AirPods Pro 2", 7999.00m, 5, ListingCondition.LikeNew, "ZYN-APP-PRO2"),            // low stock
        new(Zeynep, "Samsung 25W Type-C", 650.00m, 20, ListingCondition.New, "ZYN-SAM-25W"),
        new(Zeynep, "Anker PowerCore 10000", 1200.00m, 15, ListingCondition.Used, "ZYN-ANK-PC10K"),
        new(Zeynep, "Sony WH-1000XM5 Kablosuz Kulaklık", 11499.00m, 8, ListingCondition.New, "ZYN-SONY-XM5"),
        new(Zeynep, "Philips Airfryer XL", 3499.00m, 12, ListingCondition.New, "ZYN-PHI-AFXL"),
        new(Zeynep, "Decathlon Yoga Matı 8mm", 450.00m, 30, ListingCondition.New, "ZYN-DEC-YOGA"),
        new(Zeynep, "Nike Air Zoom Pegasus Koşu Ayakkabısı", 4299.00m, 3, ListingCondition.New, "ZYN-NIKE-PEG"), // low stock
        new(Zeynep, "Sapiens: Hayvanlardan Tanrılara", 189.90m, 40, ListingCondition.New, "ZYN-KTP-SAPIENS"),
        new(Zeynep, "Mavi Kadın Triko Kazak", 1299.00m, 18, ListingCondition.New, "ZYN-MAVI-TRK"),
        new(Zeynep, "Lacoste Erkek Polo Tişört", 2250.00m, 6, ListingCondition.New, "ZYN-LAC-POLO"),
        // Mehmet
        new(Mehmet, "Logitech MX Master 3S", 4099.00m, 0, ListingCondition.New, "MHM-LOGI-MX3S"),       // out of stock
        new(Mehmet, "AirPods Pro 2", 7799.00m, 14, ListingCondition.New, "MHM-APP-PRO2"),
        new(Mehmet, "Samsung 25W Type-C", 590.00m, 12, ListingCondition.Used, "MHM-SAM-25W"),
        new(Mehmet, "Anker PowerCore 10000", 1100.00m, 8, ListingCondition.LikeNew, "MHM-ANK-PC10K"),
        new(Mehmet, "HP 27\" IPS Monitor", 4200.00m, 7, ListingCondition.Refurbished, "MHM-HP-27IPS"),
        new(Mehmet, "Karaca Çelik Tencere Seti", 2899.00m, 9, ListingCondition.New, "MHM-KRC-TNC8"),
        new(Mehmet, "Xiaomi Robot Süpürge S10", 8999.00m, 2, ListingCondition.New, "MHM-XIA-RS10"),      // low stock
        new(Mehmet, "Adidas Antrenman Spor Çantası", 899.00m, 14, ListingCondition.New, "MHM-ADI-BAG35"),
        new(Mehmet, "Sapiens: Hayvanlardan Tanrılara", 179.90m, 25, ListingCondition.New, "MHM-KTP-SAPIENS"),
        new(Mehmet, "Simyacı", 129.90m, 25, ListingCondition.New, "MHM-KTP-SIMYACI"),
        new(Mehmet, "Suç ve Ceza", 149.90m, 30, ListingCondition.New, "MHM-KTP-SUCCEZA"),
        new(Mehmet, "Levi's 501 Erkek Kot Pantolon", 1899.00m, 20, ListingCondition.New, "MHM-LEVIS-501")
    };

    private static StockStatus ComputeStockStatus(int stock, int threshold) =>
        stock <= 0 ? StockStatus.OutOfStock
        : stock <= threshold ? StockStatus.LowStock
        : StockStatus.InStock;

    private static async Task SeedListingsAsync(ApplicationDbContext db)
    {
        var sellerEmails = ListingSpecs.Select(l => l.SellerEmail).Distinct().ToList();
        var productNames = ListingSpecs.Select(l => l.ProductName).Distinct().ToList();
        var sellers = LowestIdPerKey(
            await db.Users.IgnoreQueryFilters()
                .Where(u => u.Role == UserRole.Seller && sellerEmails.Contains(u.Email))
                .Select(u => new { u.Id, u.Email })
                .ToListAsync(),
            u => u.Email, u => u.Id);
        var products = LowestIdPerKey(
            await db.Products.IgnoreQueryFilters()
                .Where(p => productNames.Contains(p.ProductName))
                .Select(p => new { p.Id, p.ProductName })
                .ToListAsync(),
            p => p.ProductName, p => p.Id);

        foreach (var l in ListingSpecs)
        {
            if (!sellers.TryGetValue(l.SellerEmail, out var seller)) continue;
            if (!products.TryGetValue(l.ProductName, out var product)) continue;
            var sellerId = seller.Id;
            var productId = product.Id;

            var exists = await db.Listings.IgnoreQueryFilters()
                .AnyAsync(x => x.SellerId == sellerId && x.ProductId == productId);
            if (exists) continue;

            db.Listings.Add(new Listing
            {
                SellerId = sellerId,
                ProductId = productId,
                Price = l.Price,
                StockQuantity = l.Stock,
                LowStockThreshold = l.LowStockThreshold,
                TrackInventory = true,
                StockStatus = ComputeStockStatus(l.Stock, l.LowStockThreshold),
                Condition = l.Condition,
                SKU = l.Sku,
                IsActive = true
            });
        }
        await db.SaveChangesAsync();
    }

    // ------------------------------------------------------------------ carts
    // Key: UserId. Carts start empty so demo customers never hold items in their own name.
    private static async Task SeedCartsAsync(ApplicationDbContext db)
    {
        var users = await db.Users.ToListAsync();
        foreach (var u in users)
        {
            var hasCart = await db.Carts.IgnoreQueryFilters().AnyAsync(c => c.UserId == u.Id);
            if (!hasCart)
                db.Carts.Add(new Cart { UserId = u.Id });
        }
        await db.SaveChangesAsync();
    }

    // -------------------------------------------------------------- addresses
    // Key: (UserId, Title).
    private sealed record AddressSpec(
        string Email, string Title, string FullName, string Phone, string City, string District,
        string Neighborhood, string AddressLine, string PostalCode, bool IsDefault);

    private static readonly AddressSpec[] AddressSpecs =
    {
        new("ahmet@test.com", "Ev", "Ahmet Yılmaz", "05321112233", "İstanbul", "Kadıköy", "Caferağa", "Moda Cad. No:12 D:5", "34710", true),
        new("elif@test.com",  "Ev", "Elif Şahin",  "05334445566", "Ankara",   "Çankaya", "Kızılay",  "Atatürk Bulvarı No:45 D:8", "06420", true),
        new("can@test.com",   "Ev", "Can Arslan",  "05357778899", "İzmir",    "Bornova", "Kazımdirik", "Ege Üniversitesi Cad. No:3 D:2", "35040", true)
    };

    private static async Task SeedAddressesAsync(ApplicationDbContext db)
    {
        var emails = AddressSpecs.Select(a => a.Email).Distinct().ToList();
        var users = LowestIdPerKey(
            await db.Users.IgnoreQueryFilters()
                .Where(u => emails.Contains(u.Email))
                .Select(u => new { u.Id, u.Email })
                .ToListAsync(),
            u => u.Email, u => u.Id);

        foreach (var a in AddressSpecs)
        {
            if (!users.TryGetValue(a.Email, out var user)) continue;
            var userId = user.Id;

            var exists = await db.Addresses.IgnoreQueryFilters()
                .AnyAsync(x => x.UserId == userId && x.Title == a.Title);
            if (exists) continue;

            db.Addresses.Add(new Address
            {
                UserId = userId,
                Title = a.Title,
                FullName = a.FullName,
                Phone = a.Phone,
                City = a.City,
                District = a.District,
                Neighborhood = a.Neighborhood,
                AddressLine = a.AddressLine,
                PostalCode = a.PostalCode,
                IsDefault = a.IsDefault,
                Type = AddressType.Both
            });
        }
        await db.SaveChangesAsync();
    }

    // ----------------------------------------------------------------- orders
    // Key: Order.CustomerNote == "SEED-n". Each order spans both sellers; statuses are
    // coherent between the parent order, seller orders and shipments.
    private sealed record OrderLine(string SellerEmail, string ProductName, int Qty);

    private sealed record OrderSpec(
        int Number, string BuyerEmail, OrderStatus Status, int DaysAgo, string? Carrier, OrderLine[] Lines);

    private static readonly OrderSpec[] OrderSpecs =
    {
        new(1, "ahmet@test.com", OrderStatus.Delivered, 9, "Yurtiçi Kargo", new[]
        {
            new OrderLine(Zeynep, "Logitech MX Master 3S", 1), new OrderLine(Zeynep, "Sapiens: Hayvanlardan Tanrılara", 2),
            new OrderLine(Mehmet, "Samsung 25W Type-C", 1), new OrderLine(Mehmet, "Simyacı", 1)
        }),
        new(2, "elif@test.com", OrderStatus.Delivered, 7, "Aras Kargo", new[]
        {
            new OrderLine(Zeynep, "Sony WH-1000XM5 Kablosuz Kulaklık", 1),
            new OrderLine(Mehmet, "Karaca Çelik Tencere Seti", 1)
        }),
        new(3, "ahmet@test.com", OrderStatus.Shipped, 4, "Yurtiçi Kargo", new[]
        {
            new OrderLine(Zeynep, "Decathlon Yoga Matı 8mm", 2),
            new OrderLine(Mehmet, "Adidas Antrenman Spor Çantası", 1)
        }),
        new(4, "elif@test.com", OrderStatus.Confirmed, 2, null, new[]
        {
            new OrderLine(Zeynep, "Mavi Kadın Triko Kazak", 1),
            new OrderLine(Mehmet, "Levi's 501 Erkek Kot Pantolon", 1), new OrderLine(Mehmet, "Anker PowerCore 10000", 1)
        }),
        new(5, "ahmet@test.com", OrderStatus.Pending, 1, null, new[]
        {
            new OrderLine(Zeynep, "Philips Airfryer XL", 1),
            new OrderLine(Mehmet, "Suç ve Ceza", 2)
        })
    };

    private static async Task SeedOrdersAsync(ApplicationDbContext db)
    {
        var buyerEmails = OrderSpecs.Select(o => o.BuyerEmail).Distinct().ToList();
        var users = LowestIdPerKey(
            await db.Users.IgnoreQueryFilters().Where(u => buyerEmails.Contains(u.Email)).ToListAsync(),
            u => u.Email, u => u.Id);
        var listings = await db.Listings.IgnoreQueryFilters()
            .Include(l => l.Product).Include(l => l.Seller)
            .ToListAsync();

        var now = DateTime.UtcNow;

        foreach (var spec in OrderSpecs)
        {
            var note = $"SEED-{spec.Number}";
            if (await db.Orders.IgnoreQueryFilters().AnyAsync(o => o.CustomerNote == note)) continue;
            if (!users.TryGetValue(spec.BuyerEmail, out var buyer)) continue;

            var address = await db.Addresses.IgnoreQueryFilters()
                .FirstOrDefaultAsync(a => a.UserId == buyer.Id && a.IsDefault);
            if (address is null) continue;

            var resolved = spec.Lines
                .Select(l => (Line: l, Listing: listings.FirstOrDefault(x =>
                    x.Seller.Email == l.SellerEmail && x.Product.ProductName == l.ProductName)))
                .ToList();
            if (resolved.Any(r => r.Listing is null)) continue; // data not fully seeded; skip rather than half-build

            var createdAt = now.AddDays(-spec.DaysAgo);
            var paidAt = spec.Status == OrderStatus.Pending ? (DateTime?)null : createdAt.AddMinutes(10);
            var shippedAt = spec.Status is OrderStatus.Shipped or OrderStatus.Delivered ? createdAt.AddDays(1) : (DateTime?)null;
            var deliveredAt = spec.Status == OrderStatus.Delivered ? createdAt.AddDays(3) : (DateTime?)null;

            var sellerStatus = spec.Status switch
            {
                OrderStatus.Delivered => SellerOrderStatus.Delivered,
                OrderStatus.Shipped => SellerOrderStatus.Shipped,
                OrderStatus.Confirmed => SellerOrderStatus.Confirmed,
                _ => SellerOrderStatus.Placed
            };

            var order = new Order
            {
                OrderNumber = $"ORD-{createdAt:yyyyMMdd}-SEED{spec.Number:D4}",
                BuyerId = buyer.Id,
                Status = spec.Status,
                CreatedAt = createdAt,
                CustomerNote = note,
                ShippingAddressId = address.Id,
                ShippingAddressSnapshot = $"{address.FullName}, {address.AddressLine}, {address.Neighborhood}, {address.District}/{address.City}, {address.Phone}",
                BillingAddressId = address.Id,
                BillingAddressSnapshot = $"{address.FullName}, {address.AddressLine}, {address.Neighborhood}, {address.District}/{address.City}, {address.Phone}",
                PaidAt = paidAt,
                ShippedAt = shippedAt,
                DeliveredAt = deliveredAt,
                SellerOrders = new List<SellerOrder>(),
                OrderEvents = new List<OrderEvent>()
            };

            foreach (var group in resolved.GroupBy(r => r.Listing!.SellerId))
            {
                var sellerOrder = new SellerOrder
                {
                    Order = order,
                    SellerId = group.Key,
                    Status = sellerStatus,
                    CreatedAt = createdAt,
                    Items = new List<SellerOrderItem>()
                };

                foreach (var (line, listing) in group)
                {
                    sellerOrder.Items.Add(new SellerOrderItem
                    {
                        ListingId = listing!.Id,
                        ProductId = listing.ProductId,
                        ProductName = listing.Product.ProductName,
                        UnitPrice = listing.Price,
                        Quantity = line.Qty,
                        LineTotal = listing.Price * line.Qty,
                        CreatedAt = createdAt
                    });
                }
                sellerOrder.SubTotal = sellerOrder.Items.Sum(i => i.LineTotal);

                if (shippedAt is not null)
                {
                    var delivered = spec.Status == OrderStatus.Delivered;
                    var shipment = new Shipment
                    {
                        Carrier = spec.Carrier ?? "Yurtiçi Kargo",
                        TrackingNumber = $"SEED-TRK-{spec.Number:D3}-{group.Key}",
                        Status = delivered ? ShipmentStatus.Delivered : ShipmentStatus.Shipped,
                        ShippedAt = shippedAt,
                        DeliveredAt = deliveredAt,
                        CreatedAt = createdAt
                    };
                    db.ShipmentEvents.Add(new ShipmentEvent { Shipment = shipment, Status = ShipmentStatus.Created, CreatedAt = createdAt.AddHours(2), PayloadJson = "{\"by\":\"seed\"}" });
                    db.ShipmentEvents.Add(new ShipmentEvent { Shipment = shipment, Status = ShipmentStatus.Shipped, CreatedAt = shippedAt.Value, PayloadJson = "{\"by\":\"seed\"}" });
                    if (delivered)
                        db.ShipmentEvents.Add(new ShipmentEvent { Shipment = shipment, Status = ShipmentStatus.Delivered, CreatedAt = deliveredAt!.Value, PayloadJson = "{\"by\":\"seed\"}" });
                    sellerOrder.Shipment = shipment;
                }

                order.SellerOrders.Add(sellerOrder);
            }

            order.SubTotal = order.SellerOrders.Sum(s => s.SubTotal);
            order.TotalAmount = order.SubTotal + order.ShippingCost + order.TaxAmount - order.DiscountAmount;

            AddEvent(order, OrderStatus.Pending, "Order placed", createdAt, buyer.Id, "Customer");
            if (paidAt is not null)
                AddEvent(order, OrderStatus.Confirmed, "Order confirmed", paidAt.Value, null, "System");
            if (shippedAt is not null)
                AddEvent(order, OrderStatus.Shipped, "Order shipped", shippedAt.Value, null, "Seller");
            if (deliveredAt is not null)
                AddEvent(order, OrderStatus.Delivered, "Order delivered", deliveredAt.Value, null, "System");

            db.Orders.Add(order);
        }

        await db.SaveChangesAsync();
    }

    private static void AddEvent(Order order, OrderStatus status, string title, DateTime at, int? userId, string userType) =>
        order.OrderEvents.Add(new OrderEvent
        {
            Status = status,
            Title = title,
            UserId = userId,
            UserType = userType,
            CreatedAt = at
        });
}
