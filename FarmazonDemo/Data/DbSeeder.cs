using FarmazonDemo.Data;
using FarmazonDemo.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FarmazonDemo.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db)
    {
        await db.Database.MigrateAsync();

        // USERS
        if (!await db.Users.AnyAsync())
        {
            db.Users.AddRange(
                new Users { Name = "Ahmet Yılmaz", Email = "ahmet@test.com", Password = "123456", Username = "ahmety" },
                new Users { Name = "Zeynep Kaya", Email = "zeynep@test.com", Password = "123456", Username = "zeynepk" }
            );
            await db.SaveChangesAsync();
        }

        // PRODUCTS
        if (!await db.Products.AnyAsync())
        {
            db.Products.AddRange(
                new Product { ProductName = "Logitech MX Master 3S", ProductDescription = "Kablosuz mouse", ProductBarcode = "LOGI-MX3S" },
                new Product { ProductName = "AirPods Pro 2", ProductDescription = "ANC kulaklık", ProductBarcode = "APPLE-APP2" }
            );
            await db.SaveChangesAsync();
        }

        // LISTINGS
        if (!await db.Listings.AnyAsync())
        {
            var seller = await db.Users.FirstAsync();
            var product = await db.Products.FirstAsync();

            db.Listings.Add(new Listing
            {
                ProductId = product.ProductId,
                SellerId = seller.UserId,
                Price = 3999.90m,
                Stock = 10,
                Condition = "New",
                IsActive = true
            });

            await db.SaveChangesAsync();
        }
    }
}
