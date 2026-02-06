using FarmazonDemo.Services.SellerReviews;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FarmazonDemo.Controllers;

[ApiController]
[Route("api/sellers")]
public class SellerReviewController : ControllerBase
{
    private readonly ISellerReviewService _reviewService;

    public SellerReviewController(ISellerReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>
    /// Satici hakkinda degerlendirme yap
    /// </summary>
    [HttpPost("{sellerId}/reviews")]
    [Authorize]
    public async Task<IActionResult> CreateReview(int sellerId, [FromBody] CreateSellerReviewDto dto, CancellationToken ct)
    {
        dto.SellerId = sellerId;
        var review = await _reviewService.CreateReviewAsync(GetUserId(), dto, ct);
        return CreatedAtAction(nameof(GetSellerReviews), new { sellerId }, review);
    }

    /// <summary>
    /// Satici degerlendirmesini guncelle
    /// </summary>
    [HttpPut("{sellerId}/reviews/{reviewId}")]
    [Authorize]
    public async Task<IActionResult> UpdateReview(int sellerId, int reviewId, [FromBody] UpdateSellerReviewDto dto, CancellationToken ct)
    {
        var review = await _reviewService.UpdateReviewAsync(GetUserId(), reviewId, dto, ct);
        return Ok(review);
    }

    /// <summary>
    /// Satici degerlendirmesini sil
    /// </summary>
    [HttpDelete("{sellerId}/reviews/{reviewId}")]
    [Authorize]
    public async Task<IActionResult> DeleteReview(int sellerId, int reviewId, CancellationToken ct)
    {
        await _reviewService.DeleteReviewAsync(GetUserId(), reviewId, ct);
        return NoContent();
    }

    /// <summary>
    /// Saticinin degerlendirmelerini listele
    /// </summary>
    [HttpGet("{sellerId}/reviews")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSellerReviews(int sellerId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var reviews = await _reviewService.GetReviewsBySellerAsync(sellerId, page, pageSize, ct);
        return Ok(reviews);
    }

    /// <summary>
    /// Saticinin puan ozetini getir
    /// </summary>
    [HttpGet("{sellerId}/rating-summary")]
    [AllowAnonymous]
    public async Task<IActionResult> GetRatingSummary(int sellerId, CancellationToken ct)
    {
        var summary = await _reviewService.GetSellerRatingSummaryAsync(sellerId, ct);
        return Ok(summary);
    }

    /// <summary>
    /// Satici olarak degerlendirmeye yanit ver
    /// </summary>
    [HttpPost("{sellerId}/reviews/{reviewId}/reply")]
    [Authorize(Policy = "SellerOnly")]
    public async Task<IActionResult> ReplyToReview(int sellerId, int reviewId, [FromBody] ReplyDto dto, CancellationToken ct)
    {
        var review = await _reviewService.ReplyToReviewAsync(GetUserId(), reviewId, dto.Reply, ct);
        return Ok(review);
    }

    /// <summary>
    /// Degerlendirmeyi raporla
    /// </summary>
    [HttpPost("{sellerId}/reviews/{reviewId}/report")]
    [Authorize]
    public async Task<IActionResult> ReportReview(int sellerId, int reviewId, CancellationToken ct)
    {
        var review = await _reviewService.ReportReviewAsync(GetUserId(), reviewId, ct);
        return Ok(review);
    }
}

public class ReplyDto
{
    public string Reply { get; set; } = string.Empty;
}
