using FarmazonDemo.Services.Loyalty;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FarmazonDemo.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LoyaltyController : ControllerBase
{
    private readonly ILoyaltyService _loyaltyService;

    public LoyaltyController(ILoyaltyService loyaltyService)
    {
        _loyaltyService = loyaltyService;
    }

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>
    /// Kullanicinin sadakat puan bilgilerini getir
    /// </summary>
    [HttpGet("points")]
    public async Task<IActionResult> GetMyPoints(CancellationToken ct)
    {
        var points = await _loyaltyService.GetUserPointsAsync(GetUserId(), ct);
        return Ok(points);
    }

    /// <summary>
    /// Puan islem gecmisini getir
    /// </summary>
    [HttpGet("transactions")]
    public async Task<IActionResult> GetTransactions([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var transactions = await _loyaltyService.GetTransactionHistoryAsync(GetUserId(), page, pageSize, ct);
        return Ok(transactions);
    }

    /// <summary>
    /// Puanin TL karsiligini hesapla
    /// </summary>
    [HttpGet("calculate-value")]
    public async Task<IActionResult> CalculateValue([FromQuery] int points, CancellationToken ct)
    {
        var value = await _loyaltyService.CalculatePointValueAsync(points, ct);
        return Ok(new { points, valueTL = value });
    }

    /// <summary>
    /// Sipariste kazanilacak puan miktarini hesapla
    /// </summary>
    [HttpGet("estimate-earn")]
    public async Task<IActionResult> EstimateEarn([FromQuery] decimal orderAmount, CancellationToken ct)
    {
        var userPoints = await _loyaltyService.GetUserPointsAsync(GetUserId(), ct);
        if (!Enum.TryParse<Models.Entities.LoyaltyTier>(userPoints.Tier, out var tier))
            tier = Models.Entities.LoyaltyTier.Bronze;

        var points = await _loyaltyService.CalculateEarnablePointsAsync(orderAmount, tier, ct);
        return Ok(new { orderAmount, estimatedPoints = points, tier = userPoints.Tier });
    }

    /// <summary>
    /// Puan harca (siparis sirasinda kullanilir)
    /// </summary>
    [HttpPost("spend")]
    public async Task<IActionResult> SpendPoints([FromBody] SpendPointsDto dto, CancellationToken ct)
    {
        var result = await _loyaltyService.SpendPointsAsync(GetUserId(), dto.Points, dto.OrderId, ct);
        return Ok(result);
    }
}

public class SpendPointsDto
{
    public int Points { get; set; }
    public int? OrderId { get; set; }
}
