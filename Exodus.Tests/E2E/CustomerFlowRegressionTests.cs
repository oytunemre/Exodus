using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Json;
using Exodus.Data;
using Exodus.Models.Dto;
using Exodus.Models.Dto.CartDto;
using Exodus.Models.Dto.ListingDto;
using Exodus.Models.Dto.ProductDto;
using Exodus.Models.Dto.UserDto;
using Exodus.Models.Entities;
using Exodus.Models.Enums;
using Exodus.Services.Comparison;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Exodus.Tests.E2E;

/// <summary>
/// Regression coverage for defects found while exercising customer flows against SQL Server:
/// listing ownership, checkout address ownership, cancellation rollup, stale delete responses,
/// input validation and password hashing.
/// </summary>
public class CustomerFlowRegressionTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CustomerFlowRegressionTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static async Task<int> CreateProductAsync(HttpClient client, string suffix)
    {
        var response = await client.PostAsJsonAsync("/api/product", new AddProductDto
        {
            ProductName = "Regression Product " + suffix,
            ProductDescription = "Product for customer-flow regression tests",
            Barcodes = new List<string> { Guid.NewGuid().ToString("N") }
        }, TestHelper.JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProductResponseDto>(TestHelper.JsonOptions))!.Id;
    }

    private static async Task<ListingResponseDto> CreateListingAsync(HttpClient client, int productId, int sellerId, int stock = 50)
    {
        var response = await client.PostAsJsonAsync("/api/listings", new AddListingDto
        {
            ProductId = productId,
            SellerId = sellerId,
            Price = 100m,
            Stock = stock,
            Condition = ListingCondition.New
        }, TestHelper.JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ListingResponseDto>(TestHelper.JsonOptions))!;
    }

    private static async Task<int> CreateAddressAsync(HttpClient client, string suffix)
    {
        var response = await client.PostAsJsonAsync("/api/address", new CreateAddressDto
        {
            Title = "Home " + suffix,
            FullName = "Regression User " + suffix,
            Phone = "+905551234567",
            City = "Istanbul",
            District = "Kadikoy",
            Neighborhood = "Caferaga",
            AddressLine = "Regression St. No:1 " + suffix,
            PostalCode = "34710",
            IsDefault = false
        }, TestHelper.JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AddressResponseDto>(TestHelper.JsonOptions))!.Id;
    }

    // --------------------------------------------------------------- listings

    [Fact]
    public async Task Listing_OtherSeller_CannotCreateUpdateOrDelete()
    {
        var ownerClient = _factory.CreateClient();
        var owner = await TestHelper.RegisterAndLoginAsSellerAsync(ownerClient, "lstown");
        var productId = await CreateProductAsync(ownerClient, "lstown");
        var listing = await CreateListingAsync(ownerClient, productId, owner.UserId);

        var otherClient = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsSellerAsync(otherClient, "lstother");

        var update = await otherClient.PutAsJsonAsync($"/api/listings/{listing.Id}",
            new UpdateListingDto { Price = 1m, Stock = 1, IsActive = true }, TestHelper.JsonOptions);
        update.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var delete = await otherClient.DeleteAsync($"/api/listings/{listing.Id}");
        delete.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var create = await otherClient.PostAsJsonAsync("/api/listings", new AddListingDto
        {
            ProductId = productId,
            SellerId = owner.UserId,
            Price = 5m,
            Stock = 5,
            Condition = ListingCondition.New
        }, TestHelper.JsonOptions);
        create.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var persisted = await db.Listings.SingleAsync(l => l.Id == listing.Id);
        persisted.Price.Should().Be(100m);
        persisted.IsDeleted.Should().BeFalse();
        (await db.Listings.CountAsync(l => l.SellerId == owner.UserId)).Should().Be(1);
    }

    // ------------------------------------------------------------ checkout

    [Fact]
    public async Task Checkout_WithForeignBillingAddress_ReturnsBadRequestAndKeepsCart()
    {
        var client = _factory.CreateClient();
        var seller = await TestHelper.RegisterAndLoginAsSellerAsync(client, "billseller");
        var productId = await CreateProductAsync(client, "bill");
        var listing = await CreateListingAsync(client, productId, seller.UserId, stock: 10);

        var otherClient = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsCustomerAsync(otherClient, "billother");
        var foreignAddressId = await CreateAddressAsync(otherClient, "billother");

        var buyer = await TestHelper.RegisterAndLoginAsCustomerAsync(client, "billbuyer");
        var shippingId = await CreateAddressAsync(client, "billbuyer");
        (await client.PostAsJsonAsync("/api/cart/add",
            new AddToCartDto { UserId = buyer.UserId, ListingId = listing.Id, Quantity = 2 },
            TestHelper.JsonOptions)).EnsureSuccessStatusCode();

        var response = await client.PostAsJsonAsync("/api/order/checkout",
            new CreateOrderDto { ShippingAddressId = shippingId, BillingAddressId = foreignAddressId },
            TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        (await db.Orders.AnyAsync(o => o.BuyerId == buyer.UserId)).Should().BeFalse();
        (await db.Listings.SingleAsync(l => l.Id == listing.Id)).StockQuantity.Should().Be(10);
    }

    [Fact]
    public async Task CancelOrder_CancelsSellerOrders()
    {
        var client = _factory.CreateClient();
        var seller = await TestHelper.RegisterAndLoginAsSellerAsync(client, "cancelseller");
        var productId = await CreateProductAsync(client, "cancel");
        var listing = await CreateListingAsync(client, productId, seller.UserId, stock: 10);

        var buyer = await TestHelper.RegisterAndLoginAsCustomerAsync(client, "cancelbuyer");
        var shippingId = await CreateAddressAsync(client, "cancelbuyer");
        (await client.PostAsJsonAsync("/api/cart/add",
            new AddToCartDto { UserId = buyer.UserId, ListingId = listing.Id, Quantity = 1 },
            TestHelper.JsonOptions)).EnsureSuccessStatusCode();

        var checkout = await client.PostAsJsonAsync("/api/order/checkout",
            new CreateOrderDto { ShippingAddressId = shippingId }, TestHelper.JsonOptions);
        checkout.IsSuccessStatusCode.Should().BeTrue();
        var order = await checkout.Content.ReadFromJsonAsync<OrderDetailResponseDto>(TestHelper.JsonOptions);

        var cancel = await client.PostAsJsonAsync($"/api/order/{order!.Id}/cancel",
            new CancelOrderDto { Reason = CancellationReason.CustomerRequest, Note = "regression" },
            TestHelper.JsonOptions);
        cancel.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var sellerOrders = await db.SellerOrders.Where(so => so.OrderId == order.Id).ToListAsync();
        sellerOrders.Should().NotBeEmpty();
        sellerOrders.Should().OnlyContain(so => so.Status == SellerOrderStatus.Cancelled);
        (await db.Listings.SingleAsync(l => l.Id == listing.Id)).StockQuantity.Should().Be(10);
    }

    // ------------------------------------------------- stale delete responses

    [Fact]
    public async Task RemoveCartItem_ResponseExcludesRemovedItem()
    {
        var client = _factory.CreateClient();
        var seller = await TestHelper.RegisterAndLoginAsSellerAsync(client, "cartstale");
        var listingA = await CreateListingAsync(client, await CreateProductAsync(client, "cartstaleA"), seller.UserId);
        var listingB = await CreateListingAsync(client, await CreateProductAsync(client, "cartstaleB"), seller.UserId);

        var buyer = await TestHelper.RegisterAndLoginAsCustomerAsync(client, "cartstale");
        await client.PostAsJsonAsync("/api/cart/add",
            new AddToCartDto { UserId = buyer.UserId, ListingId = listingA.Id, Quantity = 1 }, TestHelper.JsonOptions);
        var addResp = await client.PostAsJsonAsync("/api/cart/add",
            new AddToCartDto { UserId = buyer.UserId, ListingId = listingB.Id, Quantity = 2 }, TestHelper.JsonOptions);
        var cart = await addResp.Content.ReadFromJsonAsync<CartResponseDto>(TestHelper.JsonOptions);
        var removedId = cart!.Items.Single(i => i.ListingId == listingB.Id).CartItemId;

        var response = await client.DeleteAsync($"/api/cart/{buyer.UserId}/item/{removedId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CartResponseDto>(TestHelper.JsonOptions);
        body!.Items.Should().ContainSingle().Which.ListingId.Should().Be(listingA.Id);
        body.CartTotal.Should().Be(100m);
    }

    [Fact]
    public async Task RemoveComparisonProduct_ResponseExcludesRemovedProduct()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsSellerAsync(client, "cmpstale");
        var productA = await CreateProductAsync(client, "cmpstaleA");
        var productB = await CreateProductAsync(client, "cmpstaleB");

        await TestHelper.RegisterAndLoginAsCustomerAsync(client, "cmpstale");
        var created = await client.PostAsJsonAsync("/api/comparison", new { Name = "stale" }, TestHelper.JsonOptions);
        var comparison = await created.Content.ReadFromJsonAsync<ComparisonResponseDto>(TestHelper.JsonOptions);
        (await client.PostAsync($"/api/comparison/{comparison!.Id}/products/{productA}", null)).EnsureSuccessStatusCode();
        (await client.PostAsync($"/api/comparison/{comparison.Id}/products/{productB}", null)).EnsureSuccessStatusCode();

        var response = await client.DeleteAsync($"/api/comparison/{comparison.Id}/products/{productB}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ComparisonResponseDto>(TestHelper.JsonOptions);
        body!.Products.Should().ContainSingle().Which.ProductId.Should().Be(productA);
    }

    // ------------------------------------------------------ recently viewed

    [Fact]
    public async Task RecentlyViewed_UnknownProduct_ReturnsNotFound()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsCustomerAsync(client, "rvmissing");

        var response = await client.PostAsync("/api/recentlyviewed/999999", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(10000)]
    public async Task RecentlyViewed_OutOfRangeCount_ReturnsOk(int count)
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsCustomerAsync(client, "rvcount" + Math.Abs(count));

        var response = await client.GetAsync($"/api/recentlyviewed?count={count}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ---------------------------------------------------------------- users

    [Fact]
    public async Task AdminUserCrud_HashesPasswordsSoUserCanLogIn()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsAdminAsync(client, "usrhash");

        var create = await client.PostAsJsonAsync("/api/user", new AdduserDto
        {
            Name = "Hashed User",
            Email = "hasheduser@example.com",
            Username = "hasheduser",
            Password = "Initial123!@#"
        }, TestHelper.JsonOptions);
        create.IsSuccessStatusCode.Should().BeTrue();
        var created = await create.Content.ReadFromJsonAsync<UserResponseDto>(TestHelper.JsonOptions);

        var anonymous = _factory.CreateClient();
        (await TestHelper.LoginAsync(anonymous, "hasheduser@example.com", "Initial123!@#")).Token
            .Should().NotBeNullOrEmpty();

        var update = await client.PutAsJsonAsync($"/api/user/{created!.Id}", new UserUpdateDto
        {
            Name = "Hashed User",
            Email = "hasheduser@example.com",
            Username = "hasheduser",
            Password = "Updated123!@#"
        }, TestHelper.JsonOptions);
        update.IsSuccessStatusCode.Should().BeTrue();

        (await TestHelper.LoginAsync(anonymous, "hasheduser@example.com", "Updated123!@#")).Token
            .Should().NotBeNullOrEmpty();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var stored = await db.Users.SingleAsync(u => u.Id == created.Id);
        stored.Password.Should().NotBe("Updated123!@#");
        BCrypt.Net.BCrypt.Verify("Updated123!@#", stored.Password).Should().BeTrue();
    }

    // ------------------------------------------------------------ two-factor

    [Theory]
    [InlineData("123456", true)]
    [InlineData("12345678", true)]
    [InlineData("12345", false)]
    [InlineData("123456789", false)]
    public void TwoFactorVerifyDto_AcceptsTotpAndBackupCodeLengths(string code, bool expectedValid)
    {
        var dto = new TwoFactorVerifyDto { Code = code };
        var results = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, validateAllProperties: true);

        valid.Should().Be(expectedValid);
    }

    // --------------------------------------------------------------- loyalty

    [Fact]
    public async Task Loyalty_NegativeCalculatorInputs_ReturnBadRequest()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsCustomerAsync(client, "loyneg");

        (await client.GetAsync("/api/loyalty/calculate-value?points=-1")).StatusCode
            .Should().Be(HttpStatusCode.BadRequest);
        (await client.GetAsync("/api/loyalty/estimate-earn?orderAmount=-1")).StatusCode
            .Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Loyalty_SpendAgainstForeignOrMissingOrder_ReturnsBadRequest()
    {
        var ownerClient = _factory.CreateClient();
        var owner = await TestHelper.RegisterAndLoginAsCustomerAsync(ownerClient, "loyorderowner");

        var client = _factory.CreateClient();
        var spender = await TestHelper.RegisterAndLoginAsCustomerAsync(client, "loyorderspender");

        int foreignOrderId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var order = new Order
            {
                OrderNumber = "ORD-LOY-" + Guid.NewGuid().ToString("N")[..6],
                BuyerId = owner.UserId,
                Status = OrderStatus.Pending
            };
            db.Orders.Add(order);
            db.Set<LoyaltyPoint>().Add(new LoyaltyPoint
            {
                UserId = spender.UserId,
                AvailablePoints = 1000,
                TotalPoints = 1000
            });
            await db.SaveChangesAsync();
            foreignOrderId = order.Id;
        }

        (await client.PostAsJsonAsync("/api/loyalty/spend", new { Points = 100, OrderId = foreignOrderId },
            TestHelper.JsonOptions)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await client.PostAsJsonAsync("/api/loyalty/spend", new { Points = 10, OrderId = 999999 },
            TestHelper.JsonOptions)).StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using var verify = _factory.Services.CreateScope();
        var verifyDb = verify.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        (await verifyDb.Set<LoyaltyPoint>().SingleAsync(l => l.UserId == spender.UserId)).AvailablePoints
            .Should().Be(1000);
    }
}
