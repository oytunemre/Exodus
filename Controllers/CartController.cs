using System.Security.Claims;
using FarmazonDemo.Models.Dto.CartDto;
using FarmazonDemo.Services.Carts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmazonDemo.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class CartController : ControllerBase
{
    private readonly ICartService _cartService;

    public CartController(ICartService cartService)
    {
        _cartService = cartService;
    }

    /// <summary>
    /// Gets the current user's ID from JWT claims
    /// </summary>
    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("User ID not found in token");

        return userId;
    }

    /// <summary>
    /// Validates that the requesting user has access to the specified userId's resources
    /// </summary>
    private void ValidateUserAccess(int requestedUserId)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId != requestedUserId)
            throw new UnauthorizedAccessException("You do not have access to this cart");
    }

    [HttpGet("{userId:int}")]
    public async Task<IActionResult> GetCart(int userId)
    {
        ValidateUserAccess(userId);
        var cart = await _cartService.GetCartAsync(userId);
        return Ok(cart);
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyCart()
    {
        var userId = GetCurrentUserId();
        var cart = await _cartService.GetCartAsync(userId);
        return Ok(cart);
    }

    [HttpPost("add")]
    public async Task<IActionResult> Add([FromBody] AddToCartDto dto)
    {
        var currentUserId = GetCurrentUserId();
        if (dto.UserId != currentUserId)
            throw new UnauthorizedAccessException("You can only add items to your own cart");

        var cart = await _cartService.AddToCartAsync(dto);
        return Ok(cart);
    }

    [HttpPut("{userId:int}/item/{cartItemId:int}")]
    public async Task<IActionResult> UpdateQuantity(
        int userId,
        int cartItemId,
        [FromBody] UpdateCartItemDto dto)
    {
        ValidateUserAccess(userId);
        var cart = await _cartService.UpdateCartItemQuantityAsync(userId, cartItemId, dto.Quantity);
        return Ok(cart);
    }

    [HttpDelete("{userId:int}/item/{cartItemId:int}")]
    public async Task<IActionResult> RemoveItem(int userId, int cartItemId)
    {
        ValidateUserAccess(userId);
        var cart = await _cartService.RemoveItemAsync(userId, cartItemId);
        return Ok(cart);
    }
}
