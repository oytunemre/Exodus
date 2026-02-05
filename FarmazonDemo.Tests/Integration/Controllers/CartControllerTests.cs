using System.Security.Claims;
using FarmazonDemo.Controllers;
using FarmazonDemo.Data;
using FarmazonDemo.Models.Dto.CartDto;
using FarmazonDemo.Services.Carts;
using FarmazonDemo.Tests.Mocks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace FarmazonDemo.Tests.Integration.Controllers;

public class CartControllerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly CartService _cartService;
    private readonly CartController _controller;

    public CartControllerTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext(Guid.NewGuid().ToString());
        TestDbContextFactory.SeedTestData(_context);
        _cartService = new CartService(_context);
        _controller = new CartController(_cartService);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private void SetUserClaims(int userId)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, $"user{userId}@example.com"),
            new Claim(ClaimTypes.Role, "Customer")
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    #region GetCart Tests

    [Fact]
    public async Task GetCart_OwnCart_ReturnsOk()
    {
        // Arrange
        var userId = 1;
        SetUserClaims(userId);

        // Act
        var result = await _controller.GetCart(userId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var cart = okResult.Value.Should().BeOfType<CartResponseDto>().Subject;
        cart.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task GetCart_OtherUserCart_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var currentUserId = 1;
        var requestedUserId = 2;
        SetUserClaims(currentUserId);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.GetCart(requestedUserId));
    }

    [Fact]
    public async Task GetMyCart_ReturnsOwnCart()
    {
        // Arrange
        var userId = 1;
        SetUserClaims(userId);

        // Act
        var result = await _controller.GetMyCart();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var cart = okResult.Value.Should().BeOfType<CartResponseDto>().Subject;
        cart.UserId.Should().Be(userId);
    }

    #endregion

    #region Add Tests

    [Fact]
    public async Task Add_OwnCart_ReturnsOk()
    {
        // Arrange
        var userId = 1;
        SetUserClaims(userId);
        var dto = new AddToCartDto
        {
            UserId = userId,
            ListingId = 1,
            Quantity = 1
        };

        // Act
        var result = await _controller.Add(dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Add_OtherUserCart_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var currentUserId = 1;
        var otherUserId = 2;
        SetUserClaims(currentUserId);
        var dto = new AddToCartDto
        {
            UserId = otherUserId, // Trying to add to other user's cart
            ListingId = 1,
            Quantity = 1
        };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.Add(dto));
    }

    #endregion

    #region UpdateQuantity Tests

    [Fact]
    public async Task UpdateQuantity_OwnCartItem_ReturnsOk()
    {
        // Arrange
        var userId = 1;
        var cartItemId = 1;
        SetUserClaims(userId);
        var dto = new UpdateCartItemDto { Quantity = 5 };

        // Act
        var result = await _controller.UpdateQuantity(userId, cartItemId, dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task UpdateQuantity_OtherUserCartItem_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var currentUserId = 1;
        var otherUserId = 2;
        var cartItemId = 1;
        SetUserClaims(currentUserId);
        var dto = new UpdateCartItemDto { Quantity = 5 };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _controller.UpdateQuantity(otherUserId, cartItemId, dto));
    }

    #endregion

    #region RemoveItem Tests

    [Fact]
    public async Task RemoveItem_OwnCartItem_ReturnsOk()
    {
        // Arrange
        var userId = 1;
        var cartItemId = 1;
        SetUserClaims(userId);

        // Act
        var result = await _controller.RemoveItem(userId, cartItemId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task RemoveItem_OtherUserCartItem_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var currentUserId = 1;
        var otherUserId = 2;
        var cartItemId = 1;
        SetUserClaims(currentUserId);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _controller.RemoveItem(otherUserId, cartItemId));
    }

    #endregion

    #region Authorization Edge Cases

    [Fact]
    public async Task GetCart_NoUserClaims_ThrowsUnauthorizedAccessException()
    {
        // Arrange - No claims set (default empty context)
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.GetCart(1));
    }

    [Fact]
    public async Task GetCart_InvalidUserIdClaim_ThrowsUnauthorizedAccessException()
    {
        // Arrange - Set invalid user ID claim
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "invalid-not-a-number")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.GetCart(1));
    }

    #endregion
}
