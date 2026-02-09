using Exodus.Services.Loyalty;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Exodus.Controllers.Admin;

[ApiController]
[Route("api/admin/loyalty")]
[Authorize(Policy = "AdminOnly")]
public class AdminLoyaltyController : ControllerBase
{
    private readonly ILoyaltyService _loyaltyService;

    public AdminLoyaltyController(ILoyaltyService loyaltyService)
    {
        _loyaltyService = loyaltyService;
    }

    /// <summary>
    /// Kullanicinin puan bilgisini getir
    /// </summary>
    [HttpGet("users/{userId}")]
    public async Task<IActionResult> GetUserPoints(int userId, CancellationToken ct)
    {
        var points = await _loyaltyService.GetUserPointsAsync(userId, ct);
        return Ok(points);
    }

    /// <summary>
    /// Kullanicinin puan islem gecmisini getir
    /// </summary>
    [HttpGet("users/{userId}/transactions")]
    public async Task<IActionResult> GetUserTransactions(int userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var transactions = await _loyaltyService.GetTransactionHistoryAsync(userId, page, pageSize, ct);
        return Ok(transactions);
    }

    /// <summary>
    /// Admin olarak puan duzenlemesi yap
    /// </summary>
    [HttpPost("users/{userId}/adjust")]
    public async Task<IActionResult> AdjustPoints(int userId, [FromBody] AdminAdjustPointsDto dto, CancellationToken ct)
    {
        var result = await _loyaltyService.AdminAdjustPointsAsync(userId, dto.Points, dto.Description, ct);
        return Ok(result);
    }

    /// <summary>
    /// En yuksek puanli kullanicilari listele
    /// </summary>
    [HttpGet("top-users")]
    public async Task<IActionResult> GetTopUsers([FromQuery] int count = 20, CancellationToken ct = default)
    {
        var users = await _loyaltyService.GetTopUsersAsync(count, ct);
        return Ok(users);
    }
}

public class AdminAdjustPointsDto
{
    public int Points { get; set; }
    public string Description { get; set; } = string.Empty;
}
