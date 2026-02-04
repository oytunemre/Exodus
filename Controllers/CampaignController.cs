using FarmazonDemo.Models.Dto.Campaign;
using FarmazonDemo.Services.Campaigns;
using FarmazonDemo.Services.Carts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FarmazonDemo.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CampaignController : ControllerBase
{
    private readonly ICampaignService _campaignService;
    private readonly ICartService _cartService;

    public CampaignController(ICampaignService campaignService, ICartService cartService)
    {
        _campaignService = campaignService;
        _cartService = cartService;
    }

    private int? GetUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return userIdClaim != null ? int.Parse(userIdClaim) : null;
    }

    /// <summary>
    /// Get all active campaigns (public)
    /// </summary>
    [HttpGet("active")]
    [AllowAnonymous]
    public async Task<ActionResult<List<CampaignDto>>> GetActiveCampaigns(
        [FromQuery] int? categoryId = null,
        CancellationToken ct = default)
    {
        var campaigns = await _campaignService.GetActiveCampaignsAsync(categoryId, ct);
        return Ok(campaigns);
    }

    /// <summary>
    /// Validate coupon code
    /// </summary>
    [HttpGet("validate-coupon")]
    [Authorize]
    public async Task<ActionResult<CampaignDto>> ValidateCoupon([FromQuery] string code, CancellationToken ct)
    {
        var userId = GetUserId()!.Value;
        var campaign = await _campaignService.ValidateCouponCodeAsync(code, userId, ct);

        if (campaign == null)
            return NotFound(new { message = "Gecersiz veya suresi dolmus kupon kodu" });

        return Ok(campaign);
    }

    /// <summary>
    /// Get applicable campaigns for current cart
    /// </summary>
    [HttpGet("applicable")]
    [Authorize]
    public async Task<ActionResult<List<CampaignDto>>> GetApplicableCampaigns(CancellationToken ct)
    {
        var userId = GetUserId()!.Value;

        // Get cart items
        var cart = await _cartService.GetCartAsync(userId);
        var cartItems = cart.Items.Select(i => new CartItemForCampaign
        {
            ListingId = i.ListingId,
            ProductId = i.ProductId,
            CategoryId = i.CategoryId,
            SellerId = i.SellerId,
            ProductName = i.ProductName,
            UnitPrice = i.UnitPrice,
            Quantity = i.Quantity
        }).ToList();

        if (!cartItems.Any())
            return Ok(new List<CampaignDto>());

        var campaigns = await _campaignService.GetApplicableCampaignsAsync(userId, cartItems, ct);
        return Ok(campaigns);
    }

    /// <summary>
    /// Calculate discount for current cart
    /// </summary>
    [HttpPost("calculate")]
    [Authorize]
    public async Task<ActionResult<CampaignApplicationResult>> CalculateDiscount(
        [FromBody] ApplyCampaignDto? dto,
        CancellationToken ct)
    {
        var userId = GetUserId()!.Value;

        // Get cart items
        var cart = await _cartService.GetCartAsync(userId);
        var cartItems = cart.Items.Select(i => new CartItemForCampaign
        {
            ListingId = i.ListingId,
            ProductId = i.ProductId,
            CategoryId = i.CategoryId,
            SellerId = i.SellerId,
            ProductName = i.ProductName,
            UnitPrice = i.UnitPrice,
            Quantity = i.Quantity
        }).ToList();

        if (!cartItems.Any())
            return BadRequest(new { message = "Sepetiniz bos" });

        var result = await _campaignService.CalculateDiscountAsync(userId, cartItems, dto?.CouponCode, ct);
        return Ok(result);
    }
}
