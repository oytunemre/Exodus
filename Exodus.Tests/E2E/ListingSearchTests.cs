using System.Globalization;
using System.Net;
using System.Text.Json;
using Exodus.Data;
using Exodus.Models.Entities;
using Exodus.Models.Enums;
using Exodus.Services.Listings;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Exodus.Tests.E2E;

public class ListingSearchTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ListingSearchTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private sealed record Seeded(int CategoryId, int SellerId, int SellerId2, List<int> ProductIds, List<int> ListingIds);

    /// <summary>
    /// Seeds one category (unique per test), two sellers and one product+listing per (name, price) pair.
    /// </summary>
    private Seeded Seed(string categoryName, params (string Name, decimal Price)[] items)
    {
        var token = Guid.NewGuid().ToString("N")[..10];
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var category = new Category { Name = categoryName, Slug = "cat-" + token };
        db.Categories.Add(category);

        Users NewSeller(string n) => new()
        {
            Name = n + " " + token,
            Email = $"{n}-{token}@test.com",
            Username = $"{n}{token}",
            Password = "x",
            Role = UserRole.Seller
        };
        var s1 = NewSeller("seller1");
        var s2 = NewSeller("seller2");
        db.Users.AddRange(s1, s2);
        db.SaveChanges();

        var productIds = new List<int>();
        var listingIds = new List<int>();
        var i = 0;
        foreach (var (name, price) in items)
        {
            var product = new Product
            {
                ProductName = name,
                ProductDescription = "Description " + token,
                CategoryId = category.Id
            };
            db.Products.Add(product);
            db.SaveChanges();
            var listing = new Listing
            {
                ProductId = product.Id,
                SellerId = i++ % 2 == 0 ? s1.Id : s2.Id,
                Price = price,
                StockQuantity = 10
            };
            db.Listings.Add(listing);
            db.SaveChanges();
            productIds.Add(product.Id);
            listingIds.Add(listing.Id);
        }

        return new Seeded(category.Id, s1.Id, s2.Id, productIds, listingIds);
    }

    private async Task<JsonElement> GetJsonAsync(string url, HttpStatusCode expected = HttpStatusCode.OK)
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync(url);
        response.StatusCode.Should().Be(expected);
        var body = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(body).RootElement.Clone();
    }

    private static List<string> Names(JsonElement root) =>
        root.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("productName").GetString()!).ToList();

    [Fact]
    public async Task Search_Anonymous_ReturnsOk()
    {
        var root = await GetJsonAsync("/api/listings/search");

        root.TryGetProperty("items", out _).Should().BeTrue();
        root.TryGetProperty("totalCount", out _).Should().BeTrue();
        root.TryGetProperty("page", out _).Should().BeTrue();
        root.TryGetProperty("pageSize", out _).Should().BeTrue();
        root.TryGetProperty("totalPages", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Search_ByQuery_MatchesProductName()
    {
        var token = Guid.NewGuid().ToString("N")[..10];
        var data = Seed("Elektronik", ($"Laptop {token}", 100m), ($"Mouse {token}", 500m), ($"Monitor {token}", 900m));

        var root = await GetJsonAsync($"/api/listings/search?search=laptop {token}&categoryId={data.CategoryId}");

        Names(root).Should().ContainSingle().Which.Should().Be($"Laptop {token}");
        root.GetProperty("totalCount").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task Search_IsCaseInsensitive_ForTurkishText()
    {
        var data = Seed("Elektronik", ("Bluetooth Kulaklık", 100m), ("Mouse", 500m), ("Monitor", 900m));

        var root = await GetJsonAsync($"/api/listings/search?search={Uri.EscapeDataString("kulaklık")}&categoryId={data.CategoryId}");

        Names(root).Should().ContainSingle().Which.Should().Be("Bluetooth Kulaklık");
    }

    [Fact]
    public async Task Search_ByPriceRange_FiltersInclusive()
    {
        var data = Seed("Elektronik", ("A", 100m), ("B", 500m), ("C", 900m));

        var root = await GetJsonAsync($"/api/listings/search?categoryId={data.CategoryId}&minPrice=500&maxPrice=900");

        root.GetProperty("totalCount").GetInt32().Should().Be(2);
        Names(root).Should().BeEquivalentTo(new[] { "B", "C" });
    }

    [Fact]
    public async Task Search_ByCategory_ReturnsOnlyCategory()
    {
        var data = Seed("Elektronik", ("A", 100m), ("B", 500m), ("C", 900m));
        Seed("Giyim", ("Other", 50m));

        var root = await GetJsonAsync($"/api/listings/search?categoryId={data.CategoryId}");

        var items = root.GetProperty("items").EnumerateArray().ToList();
        items.Should().HaveCount(3);
        items.Should().OnlyContain(i => i.GetProperty("categoryName").GetString() == "Elektronik");
        items.Should().OnlyContain(i => i.GetProperty("categoryId").GetInt32() == data.CategoryId);
    }

    [Fact]
    public async Task Search_SortByPriceAsc_IsOrdered()
    {
        var data = Seed("Elektronik", ("A", 900m), ("B", 100m), ("C", 500m));

        var asc = await GetJsonAsync($"/api/listings/search?categoryId={data.CategoryId}&sortBy=price&sortOrder=asc");
        var desc = await GetJsonAsync($"/api/listings/search?categoryId={data.CategoryId}&sortBy=price&sortOrder=desc");

        asc.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("price").GetDecimal())
            .Should().Equal(100m, 500m, 900m);
        desc.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("price").GetDecimal())
            .Should().Equal(900m, 500m, 100m);
    }

    [Fact]
    public async Task Search_ExcludesInactiveAndDeletedListings()
    {
        var data = Seed("Elektronik", ("Visible", 100m), ("Inactive", 200m), ("Deleted", 300m));
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var inactive = await db.Listings.FindAsync(data.ListingIds[1]);
            inactive!.IsActive = false;
            var deleted = await db.Listings.FindAsync(data.ListingIds[2]);
            deleted!.IsDeleted = true;
            await db.SaveChangesAsync();
        }

        var root = await GetJsonAsync($"/api/listings/search?categoryId={data.CategoryId}");

        Names(root).Should().Equal("Visible");
    }

    [Fact]
    public async Task Search_ClampsPaging()
    {
        var big = await GetJsonAsync("/api/listings/search?pageSize=1000");
        big.GetProperty("pageSize").GetInt32().Should().Be(100);

        var zero = await GetJsonAsync("/api/listings/search?page=0");
        zero.GetProperty("page").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task Search_MinGreaterThanMax_ReturnsBadRequest()
    {
        await GetJsonAsync("/api/listings/search?minPrice=900&maxPrice=100", HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Search_Item_HasContractFields()
    {
        var data = Seed("Elektronik", ("Contract Item", 100m));

        var root = await GetJsonAsync($"/api/listings/search?categoryId={data.CategoryId}");

        var item = root.GetProperty("items")[0];
        foreach (var name in new[] { "id", "productId", "productName", "productDescription", "categoryId",
                     "categoryName", "price", "stockQuantity", "stockStatus", "condition", "sellerId", "sellerName", "isActive" })
            item.TryGetProperty(name, out _).Should().BeTrue($"item should expose '{name}'");
    }

    [Fact]
    public void NormalizeSearchTerm_UnderTurkishCulture_IsCultureInvariant()
    {
        // SQL LOWER() yields "ips" for "IPS"; a tr-TR host must not turn the term into "ıps".
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("tr-TR");

            ListingFilters.NormalizeSearchTerm("  IPS ").Should().Be("ips");
            ListingFilters.NormalizeSearchTerm("Kulaklık").Should().Be("kulaklık");
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }
}
