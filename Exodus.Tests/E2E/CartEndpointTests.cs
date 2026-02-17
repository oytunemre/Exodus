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

public class CartEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CartEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<ListingResponseDto> CreateProductAndListingAsync(HttpClient client, string suffix)
    {
        // Register and login as seller
        var seller = await TestHelper.RegisterAndLoginAsSellerAsync(client, suffix);

        // Create product
        var productDto = new AddProductDto
        {
            ProductName = "Cart Test Product " + suffix,
            ProductDescription = "Product for cart tests",
            Barcodes = new List<string>()
        };
        var productResp = await client.PostAsJsonAsync("/api/product", productDto, TestHelper.JsonOptions);
        var product = await productResp.Content.ReadFromJsonAsync<ProductResponseDto>(TestHelper.JsonOptions);

        // Create listing
        var listingDto = new AddListingDto
        {
            ProductId = product!.Id,
            SellerId = seller.UserId,
            Price = 99.99m,
            Stock = 100,
            Condition = ListingCondition.New
        };
        var listingResp = await client.PostAsJsonAsync("/api/listings", listingDto, TestHelper.JsonOptions);
        return (await listingResp.Content.ReadFromJsonAsync<ListingResponseDto>(TestHelper.JsonOptions))!;
    }

    [Fact]
    public async Task GetMyCart_WithAuth_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsCustomerAsync(client, "cartget");

        var response = await client.GetAsync("/api/cart/my");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetMyCart_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/cart/my");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AddToCart_WithValidListing_ReturnsOk()
    {
        var client = _factory.CreateClient();

        // Create product and listing as seller
        var listing = await CreateProductAndListingAsync(client, "cartadd");

        // Switch to customer
        var customer = await TestHelper.RegisterAndLoginAsCustomerAsync(client, "cartadd");

        var dto = new AddToCartDto
        {
            UserId = customer.UserId,
            ListingId = listing.Id,
            Quantity = 2
        };

        var response = await client.PostAsJsonAsync("/api/cart/add", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var cart = await response.Content.ReadFromJsonAsync<CartResponseDto>(TestHelper.JsonOptions);
        cart.Should().NotBeNull();
        cart!.Items.Should().HaveCount(1);
        cart.Items[0].Quantity.Should().Be(2);
    }

    [Fact]
    public async Task AddToCart_ForDifferentUser_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsCustomerAsync(client, "cartother");

        var dto = new AddToCartDto
        {
            UserId = 99999, // Different user ID
            ListingId = 1,
            Quantity = 1
        };

        var response = await client.PostAsJsonAsync("/api/cart/add", dto, TestHelper.JsonOptions);

        // Should fail because the user cannot add to another user's cart
        response.StatusCode.Should().NotBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetCartByUserId_ForOwnCart_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var customer = await TestHelper.RegisterAndLoginAsCustomerAsync(client, "cartbyid");

        var response = await client.GetAsync($"/api/cart/{customer.UserId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AddToCart_MultipleItems_UpdatesCartTotal()
    {
        var client = _factory.CreateClient();

        // Create two products with listings from the same seller
        var seller = await TestHelper.RegisterAndLoginAsSellerAsync(client, "cartmulti");

        var product1 = new AddProductDto
        {
            ProductName = "Multi Cart Product 1",
            ProductDescription = "First product",
            Barcodes = new List<string>()
        };
        var prod1Resp = await client.PostAsJsonAsync("/api/product", product1, TestHelper.JsonOptions);
        var prod1 = await prod1Resp.Content.ReadFromJsonAsync<ProductResponseDto>(TestHelper.JsonOptions);

        var product2 = new AddProductDto
        {
            ProductName = "Multi Cart Product 2",
            ProductDescription = "Second product",
            Barcodes = new List<string>()
        };
        var prod2Resp = await client.PostAsJsonAsync("/api/product", product2, TestHelper.JsonOptions);
        var prod2 = await prod2Resp.Content.ReadFromJsonAsync<ProductResponseDto>(TestHelper.JsonOptions);

        var listing1Dto = new AddListingDto
        {
            ProductId = prod1!.Id,
            SellerId = seller.UserId,
            Price = 50.00m,
            Stock = 100,
            Condition = ListingCondition.New
        };
        var lst1Resp = await client.PostAsJsonAsync("/api/listings", listing1Dto, TestHelper.JsonOptions);
        var listing1 = await lst1Resp.Content.ReadFromJsonAsync<ListingResponseDto>(TestHelper.JsonOptions);

        var listing2Dto = new AddListingDto
        {
            ProductId = prod2!.Id,
            SellerId = seller.UserId,
            Price = 75.00m,
            Stock = 100,
            Condition = ListingCondition.New
        };
        var lst2Resp = await client.PostAsJsonAsync("/api/listings", listing2Dto, TestHelper.JsonOptions);
        var listing2 = await lst2Resp.Content.ReadFromJsonAsync<ListingResponseDto>(TestHelper.JsonOptions);

        // Switch to customer and add both items
        var customer = await TestHelper.RegisterAndLoginAsCustomerAsync(client, "cartmulti");

        await client.PostAsJsonAsync("/api/cart/add",
            new AddToCartDto { UserId = customer.UserId, ListingId = listing1!.Id, Quantity = 2 },
            TestHelper.JsonOptions);

        var response = await client.PostAsJsonAsync("/api/cart/add",
            new AddToCartDto { UserId = customer.UserId, ListingId = listing2!.Id, Quantity = 1 },
            TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var cart = await response.Content.ReadFromJsonAsync<CartResponseDto>(TestHelper.JsonOptions);
        cart!.Items.Should().HaveCount(2);
        // 2 * 50 + 1 * 75 = 175
        cart.CartTotal.Should().Be(175.00m);
    }

    [Fact]
    public async Task UpdateCartItemQuantity_ReturnsUpdatedCart()
    {
        var client = _factory.CreateClient();

        var listing = await CreateProductAndListingAsync(client, "cartqty");
        var customer = await TestHelper.RegisterAndLoginAsCustomerAsync(client, "cartqty");

        // Add to cart
        var addDto = new AddToCartDto
        {
            UserId = customer.UserId,
            ListingId = listing.Id,
            Quantity = 1
        };
        var addResp = await client.PostAsJsonAsync("/api/cart/add", addDto, TestHelper.JsonOptions);
        var cart = await addResp.Content.ReadFromJsonAsync<CartResponseDto>(TestHelper.JsonOptions);
        var cartItemId = cart!.Items[0].CartItemId;

        // Update quantity
        var updateDto = new UpdateCartItemDto { Quantity = 5 };
        var response = await client.PutAsJsonAsync(
            $"/api/cart/{customer.UserId}/item/{cartItemId}", updateDto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedCart = await response.Content.ReadFromJsonAsync<CartResponseDto>(TestHelper.JsonOptions);
        updatedCart!.Items[0].Quantity.Should().Be(5);
    }

    [Fact]
    public async Task RemoveCartItem_ReturnsCartWithoutItem()
    {
        var client = _factory.CreateClient();

        var listing = await CreateProductAndListingAsync(client, "cartrm");
        var customer = await TestHelper.RegisterAndLoginAsCustomerAsync(client, "cartrm");

        // Add to cart
        var addDto = new AddToCartDto
        {
            UserId = customer.UserId,
            ListingId = listing.Id,
            Quantity = 1
        };
        var addResp = await client.PostAsJsonAsync("/api/cart/add", addDto, TestHelper.JsonOptions);
        var cart = await addResp.Content.ReadFromJsonAsync<CartResponseDto>(TestHelper.JsonOptions);
        var cartItemId = cart!.Items[0].CartItemId;

        // Remove item
        var response = await client.DeleteAsync(
            $"/api/cart/{customer.UserId}/item/{cartItemId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedCart = await response.Content.ReadFromJsonAsync<CartResponseDto>(TestHelper.JsonOptions);
        updatedCart!.Items.Should().BeEmpty();
        updatedCart.CartTotal.Should().Be(0);
    }
}
