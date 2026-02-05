using FarmazonDemo.Models.Dto.CartDto;
using FarmazonDemo.Services.Carts;
using FarmazonDemo.Tests.Mocks;
using FluentAssertions;
using Xunit;

namespace FarmazonDemo.Tests.Unit.Services;

public class CartServiceTests : IDisposable
{
    private readonly Data.ApplicationDbContext _context;
    private readonly CartService _cartService;

    public CartServiceTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext(Guid.NewGuid().ToString());
        TestDbContextFactory.SeedTestData(_context);
        _cartService = new CartService(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task GetCartAsync_ExistingUser_ReturnsCart()
    {
        // Arrange
        var userId = 1;

        // Act
        var result = await _cartService.GetCartAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        result.Items.Should().NotBeEmpty();
        result.Items.Should().HaveCount(1);
        result.CartTotal.Should().Be(200.00m); // 2 items * 100.00
    }

    [Fact]
    public async Task GetCartAsync_NonExistingUser_ReturnsEmptyCart()
    {
        // Arrange
        var userId = 999;

        // Act
        var result = await _cartService.GetCartAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        result.Items.Should().BeEmpty();
        result.CartTotal.Should().Be(0);
    }

    [Fact]
    public async Task AddToCartAsync_ValidListing_AddsItemToCart()
    {
        // Arrange
        var dto = new AddToCartDto
        {
            UserId = 1,
            ListingId = 1,
            Quantity = 1
        };

        // Act
        var result = await _cartService.AddToCartAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().NotBeEmpty();
        // Should have updated existing item quantity
        var item = result.Items.First(i => i.ListingId == 1);
        item.Quantity.Should().Be(3); // 2 existing + 1 new
    }

    [Fact]
    public async Task AddToCartAsync_ZeroQuantity_ThrowsException()
    {
        // Arrange
        var dto = new AddToCartDto
        {
            UserId = 1,
            ListingId = 1,
            Quantity = 0
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _cartService.AddToCartAsync(dto));
    }

    [Fact]
    public async Task AddToCartAsync_NegativeQuantity_ThrowsException()
    {
        // Arrange
        var dto = new AddToCartDto
        {
            UserId = 1,
            ListingId = 1,
            Quantity = -1
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _cartService.AddToCartAsync(dto));
    }

    [Fact]
    public async Task AddToCartAsync_NonExistingListing_ThrowsKeyNotFoundException()
    {
        // Arrange
        var dto = new AddToCartDto
        {
            UserId = 1,
            ListingId = 999,
            Quantity = 1
        };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _cartService.AddToCartAsync(dto));
    }

    [Fact]
    public async Task AddToCartAsync_ExceedsStock_ThrowsException()
    {
        // Arrange
        var dto = new AddToCartDto
        {
            UserId = 1,
            ListingId = 1,
            Quantity = 100 // Stock is 50
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _cartService.AddToCartAsync(dto));
    }

    [Fact]
    public async Task UpdateCartItemQuantityAsync_ValidQuantity_UpdatesQuantity()
    {
        // Arrange
        var userId = 1;
        var cartItemId = 1;
        var newQuantity = 5;

        // Act
        var result = await _cartService.UpdateCartItemQuantityAsync(userId, cartItemId, newQuantity);

        // Assert
        result.Should().NotBeNull();
        var item = result.Items.First(i => i.CartItemId == cartItemId);
        item.Quantity.Should().Be(newQuantity);
    }

    [Fact]
    public async Task UpdateCartItemQuantityAsync_ZeroQuantity_RemovesItem()
    {
        // Arrange
        var userId = 1;
        var cartItemId = 1;
        var newQuantity = 0;

        // Act
        var result = await _cartService.UpdateCartItemQuantityAsync(userId, cartItemId, newQuantity);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().NotContain(i => i.CartItemId == cartItemId);
    }

    [Fact]
    public async Task UpdateCartItemQuantityAsync_NonExistingItem_ThrowsKeyNotFoundException()
    {
        // Arrange
        var userId = 1;
        var cartItemId = 999;
        var newQuantity = 5;

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _cartService.UpdateCartItemQuantityAsync(userId, cartItemId, newQuantity));
    }

    [Fact]
    public async Task UpdateCartItemQuantityAsync_WrongUser_ThrowsKeyNotFoundException()
    {
        // Arrange
        var userId = 2; // Different user
        var cartItemId = 1; // Belongs to user 1
        var newQuantity = 5;

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _cartService.UpdateCartItemQuantityAsync(userId, cartItemId, newQuantity));
    }

    [Fact]
    public async Task RemoveItemAsync_ExistingItem_RemovesFromCart()
    {
        // Arrange
        var userId = 1;
        var cartItemId = 1;

        // Act
        var result = await _cartService.RemoveItemAsync(userId, cartItemId);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().NotContain(i => i.CartItemId == cartItemId);
    }

    [Fact]
    public async Task RemoveItemAsync_NonExistingItem_ThrowsKeyNotFoundException()
    {
        // Arrange
        var userId = 1;
        var cartItemId = 999;

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _cartService.RemoveItemAsync(userId, cartItemId));
    }

    [Fact]
    public async Task RemoveItemAsync_WrongUser_ThrowsKeyNotFoundException()
    {
        // Arrange
        var userId = 2; // Different user
        var cartItemId = 1; // Belongs to user 1

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _cartService.RemoveItemAsync(userId, cartItemId));
    }
}
