using FarmazonDemo.Services.RecentlyViewedProducts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FarmazonDemo.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RecentlyViewedController : ControllerBase
{
    private readonly IRecentlyViewedService _recentlyViewedService;

    public RecentlyViewedController(IRecentlyViewedService recentlyViewedService)
    {
        _recentlyViewedService = recentlyViewedService;
    }

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>
    /// Urun goruntuleme kaydi olustur
    /// </summary>
    [HttpPost("{productId}")]
    public async Task<IActionResult> TrackView(int productId, CancellationToken ct)
    {
        await _recentlyViewedService.TrackViewAsync(GetUserId(), productId, ct);
        return Ok(new { message = "Goruntuleme kaydedildi" });
    }

    /// <summary>
    /// Son goruntulenen urunleri listele
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetRecentlyViewed([FromQuery] int count = 20, CancellationToken ct = default)
    {
        var items = await _recentlyViewedService.GetRecentlyViewedAsync(GetUserId(), count, ct);
        return Ok(items);
    }

    /// <summary>
    /// Goruntuleme gecmisini temizle
    /// </summary>
    [HttpDelete]
    public async Task<IActionResult> ClearHistory(CancellationToken ct)
    {
        await _recentlyViewedService.ClearHistoryAsync(GetUserId(), ct);
        return NoContent();
    }

    /// <summary>
    /// Belirli bir urunu gecmisten kaldir
    /// </summary>
    [HttpDelete("{productId}")]
    public async Task<IActionResult> RemoveFromHistory(int productId, CancellationToken ct)
    {
        await _recentlyViewedService.RemoveFromHistoryAsync(GetUserId(), productId, ct);
        return NoContent();
    }
}
