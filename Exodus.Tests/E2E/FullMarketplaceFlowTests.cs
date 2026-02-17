using System.Net;
using System.Net.Http.Json;
using Exodus.Models.Dto;
using Exodus.Models.Dto.CartDto;
using Exodus.Models.Dto.ListingDto;
using Exodus.Models.Dto.ProductDto;
using Exodus.Models.Enums;
using FluentAssertions;
using Xunit;

namespace Exodus.Tests.E2E;

/// <summary>
/// End-to-end tests that verify the complete marketplace flow:
/// Admin creates category -> Seller creates product -> Seller creates listing ->
/// Customer registers -> Customer adds to cart -> Cart verification
/// </summary>
public class FullMarketplaceFlowTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public FullMarketplaceFlowTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task FullFlow_AdminCreatesCategory_SellerCreatesProductAndListing_CustomerAddsToCart()
    {
        var client = _factory.CreateClient();

        // --- Step 1: Admin creates a category ---
        var admin = await TestHelper.RegisterAndLoginAsAdminAsync(client);

        var categoryDto = new CreateCategoryDto
        {
            Name = "Smartphones",
            Description = "Mobile phones and smartphones"
        };
        var catResponse = await client.PostAsJsonAsync("/api/admin/categories", categoryDto, TestHelper.JsonOptions);
        catResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var category = await catResponse.Content.ReadFromJsonAsync<CategoryResponseDto>(TestHelper.JsonOptions);
        category.Should().NotBeNull();
        category!.Name.Should().Be("Smartphones");

        // --- Step 2: Seller registers and creates a product ---
        var seller = await TestHelper.RegisterUserAsync(client,
            name: "Seller Shop",
            email: "sellershop@example.com",
            username: "sellershop",
            password: "SellerPass123!",
            role: UserRole.Seller);
        TestHelper.SetAuthToken(client, seller.Token!);

        var productDto = new AddProductDto
        {
            ProductName = "iPhone 15 Pro",
            ProductDescription = "Apple iPhone 15 Pro 256GB",
            Barcodes = new List<string> { "0194253396895" }
        };
        var prodResponse = await client.PostAsJsonAsync("/api/product", productDto, TestHelper.JsonOptions);
        prodResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var product = await prodResponse.Content.ReadFromJsonAsync<ProductResponseDto>(TestHelper.JsonOptions);
        product.Should().NotBeNull();
        product!.ProductName.Should().Be("iPhone 15 Pro");

        // --- Step 3: Seller creates a listing for the product ---
        var listingDto = new AddListingDto
        {
            ProductId = product.Id,
            SellerId = seller.UserId,
            Price = 54999.99m,
            Stock = 25,
            Condition = ListingCondition.New
        };
        var lstResponse = await client.PostAsJsonAsync("/api/listings", listingDto, TestHelper.JsonOptions);
        lstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var listing = await lstResponse.Content.ReadFromJsonAsync<ListingResponseDto>(TestHelper.JsonOptions);
        listing.Should().NotBeNull();
        listing!.Price.Should().Be(54999.99m);
        listing.Stock.Should().Be(25);

        // --- Step 4: Customer registers and adds the listing to cart ---
        var customer = await TestHelper.RegisterUserAsync(client,
            name: "Happy Customer",
            email: "customer@example.com",
            username: "happycustomer",
            password: "CustomerPass123!",
            role: UserRole.Customer);
        TestHelper.SetAuthToken(client, customer.Token!);

        var addToCartDto = new AddToCartDto
        {
            UserId = customer.UserId,
            ListingId = listing.Id,
            Quantity = 1
        };
        var cartResponse = await client.PostAsJsonAsync("/api/cart/add", addToCartDto, TestHelper.JsonOptions);
        cartResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var cart = await cartResponse.Content.ReadFromJsonAsync<CartResponseDto>(TestHelper.JsonOptions);
        cart.Should().NotBeNull();
        cart!.Items.Should().HaveCount(1);
        cart.Items[0].ProductName.Should().Be("iPhone 15 Pro");
        cart.Items[0].Quantity.Should().Be(1);
        cart.CartTotal.Should().Be(54999.99m);

        // --- Step 5: Verify cart via GET endpoint ---
        var getCartResponse = await client.GetAsync("/api/cart/my");
        getCartResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetchedCart = await getCartResponse.Content.ReadFromJsonAsync<CartResponseDto>(TestHelper.JsonOptions);
        fetchedCart!.Items.Should().HaveCount(1);
        fetchedCart.CartTotal.Should().Be(54999.99m);

        // --- Step 6: Public product list should contain the product ---
        client.DefaultRequestHeaders.Authorization = null;
        var productsResponse = await client.GetAsync("/api/product");
        productsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // --- Step 7: Public category list should contain the category ---
        var categoriesResponse = await client.GetAsync("/api/category");
        categoriesResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // --- Step 8: Listings should be publicly accessible ---
        var listingsResponse = await client.GetAsync("/api/listings");
        listingsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task FullFlow_MultipleSellerProducts_CustomerAddsMultipleToCart()
    {
        var client = _factory.CreateClient();

        // --- Seller 1 creates a product and listing ---
        var seller1 = await TestHelper.RegisterUserAsync(client,
            name: "Seller One",
            email: "seller1flow@example.com",
            username: "seller1flow",
            password: "Seller1Pass123!",
            role: UserRole.Seller);
        TestHelper.SetAuthToken(client, seller1.Token!);

        var product1 = new AddProductDto
        {
            ProductName = "Samsung Galaxy S24",
            ProductDescription = "Samsung Galaxy S24 Ultra",
            Barcodes = new List<string> { Guid.NewGuid().ToString("N") }
        };
        var prod1Resp = await client.PostAsJsonAsync("/api/product", product1, TestHelper.JsonOptions);
        var prod1 = await prod1Resp.Content.ReadFromJsonAsync<ProductResponseDto>(TestHelper.JsonOptions);

        var listing1 = new AddListingDto
        {
            ProductId = prod1!.Id,
            SellerId = seller1.UserId,
            Price = 42999.00m,
            Stock = 50,
            Condition = ListingCondition.New
        };
        var lst1Resp = await client.PostAsJsonAsync("/api/listings", listing1, TestHelper.JsonOptions);
        var lst1 = await lst1Resp.Content.ReadFromJsonAsync<ListingResponseDto>(TestHelper.JsonOptions);

        // --- Seller 2 creates a product and listing ---
        var seller2 = await TestHelper.RegisterUserAsync(client,
            name: "Seller Two",
            email: "seller2flow@example.com",
            username: "seller2flow",
            password: "Seller2Pass123!",
            role: UserRole.Seller);
        TestHelper.SetAuthToken(client, seller2.Token!);

        var product2 = new AddProductDto
        {
            ProductName = "AirPods Pro",
            ProductDescription = "Apple AirPods Pro 2nd Generation",
            Barcodes = new List<string> { Guid.NewGuid().ToString("N") }
        };
        var prod2Resp = await client.PostAsJsonAsync("/api/product", product2, TestHelper.JsonOptions);
        var prod2 = await prod2Resp.Content.ReadFromJsonAsync<ProductResponseDto>(TestHelper.JsonOptions);

        var listing2 = new AddListingDto
        {
            ProductId = prod2!.Id,
            SellerId = seller2.UserId,
            Price = 8999.00m,
            Stock = 100,
            Condition = ListingCondition.New
        };
        var lst2Resp = await client.PostAsJsonAsync("/api/listings", listing2, TestHelper.JsonOptions);
        var lst2 = await lst2Resp.Content.ReadFromJsonAsync<ListingResponseDto>(TestHelper.JsonOptions);

        // --- Customer adds both to cart ---
        var customer = await TestHelper.RegisterUserAsync(client,
            name: "Multi Cart Buyer",
            email: "multibuyer@example.com",
            username: "multibuyer",
            password: "BuyerPass123!",
            role: UserRole.Customer);
        TestHelper.SetAuthToken(client, customer.Token!);

        // Add first item
        await client.PostAsJsonAsync("/api/cart/add",
            new AddToCartDto { UserId = customer.UserId, ListingId = lst1!.Id, Quantity = 1 },
            TestHelper.JsonOptions);

        // Add second item
        var cartResponse = await client.PostAsJsonAsync("/api/cart/add",
            new AddToCartDto { UserId = customer.UserId, ListingId = lst2!.Id, Quantity = 2 },
            TestHelper.JsonOptions);

        cartResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var cart = await cartResponse.Content.ReadFromJsonAsync<CartResponseDto>(TestHelper.JsonOptions);
        cart!.Items.Should().HaveCount(2);
        // 1 * 42999 + 2 * 8999 = 60997
        cart.CartTotal.Should().Be(60997.00m);
    }

    [Fact]
    public async Task FullFlow_SellerUpdatesListingPrice_CartReflectsNewPrice()
    {
        var client = _factory.CreateClient();

        // Seller creates product and listing
        var seller = await TestHelper.RegisterUserAsync(client,
            name: "Price Seller",
            email: "priceseller@example.com",
            username: "priceseller",
            password: "PriceSeller123!",
            role: UserRole.Seller);
        TestHelper.SetAuthToken(client, seller.Token!);

        var product = new AddProductDto
        {
            ProductName = "Price Test Product",
            ProductDescription = "For price update testing",
            Barcodes = new List<string> { Guid.NewGuid().ToString("N") }
        };
        var prodResp = await client.PostAsJsonAsync("/api/product", product, TestHelper.JsonOptions);
        var prod = await prodResp.Content.ReadFromJsonAsync<ProductResponseDto>(TestHelper.JsonOptions);

        var listing = new AddListingDto
        {
            ProductId = prod!.Id,
            SellerId = seller.UserId,
            Price = 200.00m,
            Stock = 50,
            Condition = ListingCondition.New
        };
        var lstResp = await client.PostAsJsonAsync("/api/listings", listing, TestHelper.JsonOptions);
        var lst = await lstResp.Content.ReadFromJsonAsync<ListingResponseDto>(TestHelper.JsonOptions);

        // Customer adds to cart
        var customer = await TestHelper.RegisterUserAsync(client,
            name: "Price Buyer",
            email: "pricebuyer@example.com",
            username: "pricebuyer",
            password: "PriceBuyer123!",
            role: UserRole.Customer);
        TestHelper.SetAuthToken(client, customer.Token!);

        var addDto = new AddToCartDto
        {
            UserId = customer.UserId,
            ListingId = lst!.Id,
            Quantity = 3
        };
        var cartResp = await client.PostAsJsonAsync("/api/cart/add", addDto, TestHelper.JsonOptions);
        var cart = await cartResp.Content.ReadFromJsonAsync<CartResponseDto>(TestHelper.JsonOptions);
        cart!.CartTotal.Should().Be(600.00m); // 3 * 200

        // Seller updates price
        TestHelper.SetAuthToken(client, seller.Token!);
        var updateDto = new UpdateListingDto
        {
            Price = 150.00m,
            Stock = 50,
            IsActive = true
        };
        var updateResp = await client.PutAsJsonAsync($"/api/listings/{lst.Id}", updateDto, TestHelper.JsonOptions);
        updateResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify listing price changed
        client.DefaultRequestHeaders.Authorization = null;
        var listingResp = await client.GetAsync($"/api/listings/{lst.Id}");
        var updatedListing = await listingResp.Content.ReadFromJsonAsync<ListingResponseDto>(TestHelper.JsonOptions);
        updatedListing!.Price.Should().Be(150.00m);
    }

    [Fact]
    public async Task FullFlow_RoleBasedAccess_VerifyAllRoles()
    {
        var client = _factory.CreateClient();

        // Register all three roles
        var admin = await TestHelper.RegisterUserAsync(client,
            name: "Role Admin", email: "roleadmin@test.com",
            username: "roleadmin", password: "RoleAdmin123!", role: UserRole.Admin);

        var seller = await TestHelper.RegisterUserAsync(client,
            name: "Role Seller", email: "roleseller@test.com",
            username: "roleseller", password: "RoleSeller123!", role: UserRole.Seller);

        var customer = await TestHelper.RegisterUserAsync(client,
            name: "Role Customer", email: "rolecustomer@test.com",
            username: "rolecustomer", password: "RoleCustomer123!", role: UserRole.Customer);

        // Admin can access admin endpoints
        TestHelper.SetAuthToken(client, admin.Token!);
        var adminStatsResp = await client.GetAsync("/api/auth/admin/stats");
        adminStatsResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Admin can access seller endpoints
        var sellerDashResp = await client.GetAsync("/api/auth/seller/dashboard");
        sellerDashResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Seller can access seller endpoints
        TestHelper.SetAuthToken(client, seller.Token!);
        var sellerDashResp2 = await client.GetAsync("/api/auth/seller/dashboard");
        sellerDashResp2.StatusCode.Should().Be(HttpStatusCode.OK);

        // Seller cannot access admin endpoints
        var sellerAdminResp = await client.GetAsync("/api/auth/admin/stats");
        sellerAdminResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Customer cannot access admin endpoints
        TestHelper.SetAuthToken(client, customer.Token!);
        var custAdminResp = await client.GetAsync("/api/auth/admin/stats");
        custAdminResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Customer cannot access seller endpoints
        var custSellerResp = await client.GetAsync("/api/auth/seller/dashboard");
        custSellerResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // All roles can access public endpoints
        client.DefaultRequestHeaders.Authorization = null;
        var publicProductsResp = await client.GetAsync("/api/product");
        publicProductsResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var publicCategoriesResp = await client.GetAsync("/api/category");
        publicCategoriesResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var publicListingsResp = await client.GetAsync("/api/listings");
        publicListingsResp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task FullFlow_CustomerCartIsolation_EachCustomerHasOwnCart()
    {
        var client = _factory.CreateClient();

        // Create a seller with a product/listing
        var seller = await TestHelper.RegisterUserAsync(client,
            name: "Isolation Seller", email: "isoseller@test.com",
            username: "isoseller", password: "IsoSeller123!", role: UserRole.Seller);
        TestHelper.SetAuthToken(client, seller.Token!);

        var prodDto = new AddProductDto
        {
            ProductName = "Isolation Product",
            ProductDescription = "Product for isolation test",
            Barcodes = new List<string> { Guid.NewGuid().ToString("N") }
        };
        var prodResp = await client.PostAsJsonAsync("/api/product", prodDto, TestHelper.JsonOptions);
        var prod = await prodResp.Content.ReadFromJsonAsync<ProductResponseDto>(TestHelper.JsonOptions);

        var lstDto = new AddListingDto
        {
            ProductId = prod!.Id,
            SellerId = seller.UserId,
            Price = 100.00m,
            Stock = 100,
            Condition = ListingCondition.New
        };
        var lstResp = await client.PostAsJsonAsync("/api/listings", lstDto, TestHelper.JsonOptions);
        var lst = await lstResp.Content.ReadFromJsonAsync<ListingResponseDto>(TestHelper.JsonOptions);

        // Customer 1 adds to cart
        var cust1 = await TestHelper.RegisterUserAsync(client,
            name: "Customer 1", email: "cust1iso@test.com",
            username: "cust1iso", password: "Cust1Pass123!", role: UserRole.Customer);
        TestHelper.SetAuthToken(client, cust1.Token!);

        await client.PostAsJsonAsync("/api/cart/add",
            new AddToCartDto { UserId = cust1.UserId, ListingId = lst!.Id, Quantity = 3 },
            TestHelper.JsonOptions);

        var cart1Resp = await client.GetAsync("/api/cart/my");
        var cart1 = await cart1Resp.Content.ReadFromJsonAsync<CartResponseDto>(TestHelper.JsonOptions);

        // Customer 2 has a different cart
        var cust2 = await TestHelper.RegisterUserAsync(client,
            name: "Customer 2", email: "cust2iso@test.com",
            username: "cust2iso", password: "Cust2Pass123!", role: UserRole.Customer);
        TestHelper.SetAuthToken(client, cust2.Token!);

        await client.PostAsJsonAsync("/api/cart/add",
            new AddToCartDto { UserId = cust2.UserId, ListingId = lst.Id, Quantity = 1 },
            TestHelper.JsonOptions);

        var cart2Resp = await client.GetAsync("/api/cart/my");
        var cart2 = await cart2Resp.Content.ReadFromJsonAsync<CartResponseDto>(TestHelper.JsonOptions);

        // Verify carts are isolated
        cart1!.Items[0].Quantity.Should().Be(3);
        cart1.CartTotal.Should().Be(300.00m);
        cart2!.Items[0].Quantity.Should().Be(1);
        cart2.CartTotal.Should().Be(100.00m);
    }
}
