using FarmazonDemo.Models.Entities;
using Microsoft.EntityFrameworkCore;
using FarmazonDemo.Models.Enums;

namespace FarmazonDemo.Data;

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

        // fresh load
        var users = await db.Users.ToListAsync();


        // -------------------------
        // 2) PRODUCTS + BARCODES (Products>=5, Barcodes>=5)
        // -------------------------
        var productSpecs = new[]
        {
            new {
                Name = "Logitech MX Master 3S",
                Desc = "Kablosuz mouse",
                Barcodes = new[] { "LOGI-MX3S", "LOGI-MX3S-ALT" }
            },
            new {
                Name = "AirPods Pro 2",
                Desc = "ANC kulaklık",
                Barcodes = new[] { "APPLE-APP2", "APPLE-APP2-ALT" }
            },
            new {
                Name = "Samsung 25W Type-C Hızlı Şarj",
                Desc = "Telefon adaptörü",
                Barcodes = new[] { "SAM-25W-TR", "SAM-25W-EU" }
            },
            new {
                Name = "Anker PowerCore 10000",
                Desc = "Powerbank",
                Barcodes = new[] { "ANK-PC10K", "ANK-PC10K-ALT" }
            },
            new {
                Name = "HP 27\" IPS Monitör",
                Desc = "Full HD monitör",
                Barcodes = new[] { "HP-27IPS-FHD", "HP-27IPS-FHD-ALT" }
            }
        };

        foreach (var spec in productSpecs)
        {
            var product = await db.Products
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.ProductName == spec.Name);

            if (product is null)
            {
                // Yeni ürün + barkodlar
                product = new Product
                {
                    ProductName = spec.Name,
                    ProductDescription = spec.Desc,
                    Barcodes = spec.Barcodes.Select(b => new ProductBarcode { Barcode = b }).ToList()
                };
                db.Products.Add(product);
                await db.SaveChangesAsync(); // Id lazım olabilir
            }
            else
            {
                // Ürün silinmişse geri al
                if (product.IsDeleted)
                {
                    product.IsDeleted = false;
                    product.DeletedDate = null;
                }

                // Eksik barkodları tamamla / silinmişse restore et
                foreach (var barcode in spec.Barcodes)
                {
                    var existing = await db.ProductBarcodes
                        .IgnoreQueryFilters()
                        .FirstOrDefaultAsync(pb => pb.Barcode == barcode);

                    if (existing is null)
                    {
                        db.ProductBarcodes.Add(new ProductBarcode
                        {
                            ProductId = product.Id,
                            Barcode = barcode
                        });
                    }
                    else
                    {
                        // Aynı barcode var ama silinmişse restore
                        if (existing.IsDeleted)
                        {
                            existing.IsDeleted = false;
                            existing.DeletedDate = null;
                        }

                        // Barcode başka ürüne bağlıysa seed'i bozmayalım:
                        // Unique index çakışmasın diye burayı dokunmadan geçiyoruz.
                    }
                }

                await db.SaveChangesAsync();
            }
        }

        var products = await db.Products.ToListAsync();


        // -------------------------
        // 3) LISTINGS (>=5)
        // -------------------------
        // 7 listing ile garantiye alıyorum
        var listingsPlan = new[]
        {
            new { SellerIndex = 0, ProductIndex = 0, Price = 3999.90m, Stock = 10, Condition = ListingCondition.New,        IsActive = true  },
            new { SellerIndex = 1, ProductIndex = 1, Price = 7999.00m, Stock = 5,  Condition = ListingCondition.LikeNew,    IsActive = true  },
            new { SellerIndex = 2, ProductIndex = 2, Price = 650.00m,  Stock = 20, Condition = ListingCondition.New,        IsActive = true  },
            new { SellerIndex = 3, ProductIndex = 3, Price = 1200.00m, Stock = 15, Condition = ListingCondition.Used,       IsActive = true  },
            new { SellerIndex = 4, ProductIndex = 4, Price = 4200.00m, Stock = 7,  Condition = ListingCondition.Refurbished,IsActive = true  },
            new { SellerIndex = 0, ProductIndex = 2, Price = 590.00m,  Stock = 12, Condition = ListingCondition.Used,       IsActive = true  },
            new { SellerIndex = 1, ProductIndex = 3, Price = 1100.00m, Stock = 8,  Condition = ListingCondition.LikeNew,    IsActive = true  }
        };

        foreach (var l in listingsPlan)
        {
            var sellerId = users[Math.Min(l.SellerIndex, users.Count - 1)].Id;
            var productId = products[Math.Min(l.ProductIndex, products.Count - 1)].Id;

            // Aynı seller + product için listing var mı? (seed tekrarında duplicate basmasın)
            var exists = await db.Listings.IgnoreQueryFilters()
                .AnyAsync(x => x.SellerId == sellerId && x.ProductId == productId);

            if (!exists)
            {
                db.Listings.Add(new Listing
                {
                    SellerId = sellerId,
                    ProductId = productId,
                    Price = l.Price,
                    Stock = l.Stock,
                    Condition = l.Condition,
                    IsActive = l.IsActive
                });
            }
        }
        await db.SaveChangesAsync();

        var listings = await db.Listings.ToListAsync();


        // -------------------------
        // 4) CARTS (>=5)
        // -------------------------
        foreach (var u in users)
        {
            var hasCart = await db.Carts.IgnoreQueryFilters().AnyAsync(c => c.UserId == u.Id);
            if (!hasCart)
            {
                db.Carts.Add(new Cart { UserId = u.Id });
            }
        }
        await db.SaveChangesAsync();

        var carts = await db.Carts.ToListAsync();


        // -------------------------
        // 5) CART ITEMS (>=5)
        // -------------------------
        // Her cart'a 1-2 item verelim -> toplamda 8 item garanti
        var targetItems = new List<(int CartIndex, int ListingIndex, int Quantity)>
        {
            (0, 0, 1),
            (0, 1, 2),

            (1, 2, 1),
            (1, 3, 1),

            (2, 4, 1),

            (3, 5, 2),

            (4, 6, 1),

            // ekstra (eğer listing sayısı yetiyorsa)
            (2, 1, 1)
        };

        foreach (var (cartIndex, listingIndex, quantity) in targetItems)
        {
            if (carts.Count == 0 || listings.Count == 0) break;

            var cart = carts[Math.Min(cartIndex, carts.Count - 1)];
            var listing = listings[Math.Min(listingIndex, listings.Count - 1)];

            // Unique (CartId, ListingId) var -> aynı ikiliyi tekrar ekleme
            var exists = await db.CartItems.IgnoreQueryFilters()
                .AnyAsync(ci => ci.CartId == cart.Id && ci.ListingId == listing.Id);

            if (exists) continue;

            // stok 0 ise bile demo: 1 koy (istersen burada stok kontrolü koyarız)
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
    }
}
