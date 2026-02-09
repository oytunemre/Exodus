using Exodus.Services.SellerReviews;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Exodus.Controllers.Admin;

[ApiController]
[Route("api/admin/seller-reviews")]
[Authorize(Policy = "AdminOnly")]
public class AdminSellerReviewController : ControllerBase
{
    private readonly ISellerReviewService _reviewService;

    public AdminSellerReviewController(ISellerReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    /// <summary>
    /// Saticinin degerlendirmelerini admin olarak goruntule
    /// </summary>
    [HttpGet("seller/{sellerId}")]
    public async Task<IActionResult> GetSellerReviews(int sellerId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var reviews = await _reviewService.GetReviewsBySellerAsync(sellerId, page, pageSize, ct);
        return Ok(reviews);
    }

    /// <summary>
    /// Saticinin puan ozetini getir
    /// </summary>
    [HttpGet("seller/{sellerId}/summary")]
    public async Task<IActionResult> GetSellerSummary(int sellerId, CancellationToken ct)
    {
        var summary = await _reviewService.GetSellerRatingSummaryAsync(sellerId, ct);
        return Ok(summary);
    }

    /// <summary>
    /// Degerlendirmeyi moderasyon yap (gizle, kaldir, onayla)
    /// </summary>
    [HttpPatch("{reviewId}/moderate")]
    public async Task<IActionResult> ModerateReview(int reviewId, [FromBody] ModerateSellerReviewDto dto, CancellationToken ct)
    {
        var review = await _reviewService.ModerateReviewAsync(reviewId, dto.Status, ct);
        return Ok(review);
    }
}

public class ModerateSellerReviewDto
{
    public string Status { get; set; } = string.Empty; // Active, Hidden, Removed
}
