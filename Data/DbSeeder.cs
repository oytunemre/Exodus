using Exodus.Models.Entities;
using Exodus.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace Exodus.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db)
    {
        await db.Database.MigrateAsync();

        // -------------------------
        // 1) USERS (>=5)
        // -------------------------
        var usersToSeed = new List<Users>
        {
            new Users { Name = "Ahmet Yılmaz",  Email = "ahmet@test.com",  Password = "123456", Username = "ahmety" },
            new Users { Name = "Zeynep Kaya",   Email = "zeynep@test.com", Password = "123456", Username = "zeynepk" },
            new Users { Name = "Mehmet Demir",  Email = "mehmet@test.com", Password = "123456", Username = "mehmetd" },
            new Users { Name = "Elif Şahin",    Email = "elif@test.com",   Password = "123456", Username = "elifs" },
            new Users { Name = "Can Arslan",    Email = "can@test.com",    Password = "123456", Username = "canars" }
        };

        foreach (var u in usersToSeed)
        {
            var exists = await db.Users.IgnoreQueryFilters().AnyAsync(x => x.Email == u.Email);
            if (!exists) db.Users.Add(u);
        }
        await db.SaveChangesAsync();

        var users = await db.Users.ToListAsync();
        if (users.Count < 2) return; // güvenlik


        // -------------------------
        // 2) PRODUCTS + BARCODES (>=5)
        // -------------------------
        var productSpecs = new[]
        {
            new { Name = "Logitech MX Master 3S", Desc = "Kablosuz mouse", Barcodes = new[] { "LOGI-MX3S", "LOGI-MX3S-ALT" } },
            new { Name = "AirPods Pro 2",         Desc = "ANC kulaklık",  Barcodes = new[] { "APPLE-APP2", "APPLE-APP2-ALT" } },
            new { Name = "Samsung 25W Type-C",    Desc = "Telefon adaptörü", Barcodes = new[] { "SAM-25W-TR", "SAM-25W-EU" } },
            new { Name = "Anker PowerCore 10000", Desc = "Powerbank", Barcodes = new[] { "ANK-PC10K", "ANK-PC10K-ALT" } },
            new { Name = "HP 27\" IPS Monitor",   Desc = "Full HD monitör", Barcodes = new[] { "HP-27IPS-FHD", "HP-27IPS-FHD-ALT" } }
        };

        foreach (var spec in productSpecs)
        {
            var product = await db.Products
                .IgnoreQueryFilters()
                .Include(p => p.Barcodes)
                .FirstOrDefaultAsync(p => p.ProductName == spec.Name);

            if (product is null)
            {
                product = new Product
                {
                    ProductName = spec.Name,
                    ProductDescription = spec.Desc,
                    Barcodes = spec.Barcodes.Select(b => new ProductBarcode { Barcode = b }).ToList()
                };
                db.Products.Add(product);
            }
            else
            {
                // restore soft delete
                if (product.IsDeleted)
                {
                    product.IsDeleted = false;
                    product.DeletedDate = null;
                }

                // eksik barkodları tamamla
                var existingBarcodes = product.Barcodes.Select(x => x.Barcode).ToHashSet();
                foreach (var bc in spec.Barcodes)
                {
                    if (!existingBarcodes.Contains(bc))
                        product.Barcodes.Add(new ProductBarcode { Barcode = bc });
                }
            }
        }
        await db.SaveChangesAsync();

        var products = await db.Products.ToListAsync();
        if (products.Count < 3) return;


        // -------------------------
        // 3) LISTINGS (>=5)
        // -------------------------
        var listingsPlan = new[]
        {
            new { SellerIndex = 0, ProductIndex = 0, Price = 3999.90m, Stock = 10, Condition = ListingCondition.New,         IsActive = true },
            new { SellerIndex = 1, ProductIndex = 1, Price = 7999.00m, Stock = 5,  Condition = ListingCondition.LikeNew,     IsActive = true },
            new { SellerIndex = 2, ProductIndex = 2, Price = 650.00m,  Stock = 20, Condition = ListingCondition.New,         IsActive = true },
            new { SellerIndex = 3, ProductIndex = 3, Price = 1200.00m, Stock = 15, Condition = ListingCondition.Used,        IsActive = true },
            new { SellerIndex = 4, ProductIndex = 4, Price = 4200.00m, Stock = 7,  Condition = ListingCondition.Refurbished, IsActive = true },
            new { SellerIndex = 0, ProductIndex = 2, Price = 590.00m,  Stock = 12, Condition = ListingCondition.Used,        IsActive = true },
            new { SellerIndex = 1, ProductIndex = 3, Price = 1100.00m, Stock = 8,  Condition = ListingCondition.LikeNew,     IsActive = true }
        };

        foreach (var l in listingsPlan)
        {
            var sellerId = users[Math.Min(l.SellerIndex, users.Count - 1)].Id;
            var productId = products[Math.Min(l.ProductIndex, products.Count - 1)].Id;

            var exists = await db.Listings.IgnoreQueryFilters()
                .AnyAsync(x => x.SellerId == sellerId && x.ProductId == productId);

            if (!exists)
            {
                db.Listings.Add(new Listing
                {
                    SellerId = sellerId,
                    ProductId = productId,
                    Price = l.Price,
                    StockQuantity = l.Stock,
                    Condition = l.Condition,
                    IsActive = l.IsActive
                });
            }
        }
        await db.SaveChangesAsync();

        var listings = await db.Listings.ToListAsync();
        if (listings.Count < 2) return;


        // -------------------------
        // 4) CARTS (>=users)
        // -------------------------
        foreach (var u in users)
        {
            var hasCart = await db.Carts.IgnoreQueryFilters().AnyAsync(c => c.UserId == u.Id);
            if (!hasCart)
                db.Carts.Add(new Cart { UserId = u.Id });
        }
        await db.SaveChangesAsync();

        var carts = await db.Carts.ToListAsync();
        if (carts.Count == 0) return;


        // -------------------------
        // 5) CART ITEMS (>=5)
        // -------------------------
        var targetItems = new List<(int CartIndex, int ListingIndex, int Quantity)>
        {
            (0, 0, 1),
            (0, 1, 2),
            (1, 2, 1),
            (1, 3, 1),
            (2, 4, 1),
            (3, 5, 2),
            (4, 6, 1),
            (2, 1, 1)
        };

        foreach (var (cartIndex, listingIndex, quantity) in targetItems)
        {
            if (carts.Count == 0 || listings.Count == 0) break;

            var cart = carts[Math.Min(cartIndex, carts.Count - 1)];
            var listing = listings[Math.Min(listingIndex, listings.Count - 1)];

            var exists = await db.CartItems.IgnoreQueryFilters()
                .AnyAsync(ci => ci.CartId == cart.Id && ci.ListingId == listing.Id);

            if (exists) continue;

            var finalQty = quantity <= 0 ? 1 : quantity;

            db.CartItems.Add(new CartItem
            {
                CartId = cart.Id,
                ListingId = listing.Id,
                Quantity = finalQty,
                UnitPrice = listing.Price
            });
        }

        await db.SaveChangesAsync();


        // -------------------------
        // 6) ORDERS + SELLER ORDERS + ITEMS + SHIPMENT (1 demo order)
        // -------------------------
        var hasAnyOrder = await db.Orders.IgnoreQueryFilters().AnyAsync();
        if (hasAnyOrder) return;

        var buyer = users[0];

        // Order toplamı sellerOrders'tan hesaplanacak
        var order = new Order
        {
            OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMddHHmmss}",
            BuyerId = buyer.Id,
            Status = OrderStatus.Pending,
            SellerOrders = new List<SellerOrder>() // null patlamasın
        };

        var orderListings = new[]
        {
            new { Listing = listings[0], Qty = 1 },
            new { Listing = listings[1], Qty = 2 }
        };

        var groups = orderListings.GroupBy(x => x.Listing.SellerId);

        foreach (var g in groups)
        {
            var sellerOrder = new SellerOrder
            {
                Order = order, // ilişkiyi kur
                SellerId = g.Key,
                Status = SellerOrderStatus.Confirmed,
                Items = new List<SellerOrderItem>() // null patlamasın
            };

            foreach (var item in g)
            {
                var productName = products.First(p => p.Id == item.Listing.ProductId).ProductName;

                sellerOrder.Items.Add(new SellerOrderItem
                {
                    ListingId = item.Listing.Id,
                    ProductId = item.Listing.ProductId,
                    ProductName = productName,
                    UnitPrice = item.Listing.Price,
                    Quantity = item.Qty,
                    LineTotal = item.Listing.Price * item.Qty
                });
            }

            sellerOrder.SubTotal = sellerOrder.Items.Sum(x => x.LineTotal);

            // Shipment (MVP: Created)
            sellerOrder.Shipment = new Shipment
            {
                Carrier = "Yurtiçi Kargo",
                TrackingNumber = "SEED-TRK-001",
                Status = ShipmentStatus.Created
            };

            order.SellerOrders.Add(sellerOrder);
        }

        order.TotalAmount = order.SellerOrders.Sum(x => x.SubTotal);

        db.Orders.Add(order);
        await db.SaveChangesAsync();
    }
}
