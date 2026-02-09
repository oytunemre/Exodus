using Exodus.Data;
using Exodus.Models.Entities;
using Exodus.Services.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Exodus.Controllers.Admin;

[Route("api/admin/reviews")]
[ApiController]
[Authorize(Policy = "AdminOnly")]
public class AdminReviewController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public AdminReviewController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult> GetReviews(
        [FromQuery] ReviewStatus? status = null,
        [FromQuery] ReviewType? type = null,
        [FromQuery] int? productId = null,
        [FromQuery] int? sellerId = null,
        [FromQuery] int? minRating = null,
        [FromQuery] int? maxRating = null,
        [FromQuery] bool? hasReports = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _db.Set<Review>().Include(r => r.User).Include(r => r.Product).AsQueryable();

        if (status.HasValue) query = query.Where(r => r.Status == status.Value);
        if (type.HasValue) query = query.Where(r => r.Type == type.Value);
        if (productId.HasValue) query = query.Where(r => r.ProductId == productId.Value);
        if (sellerId.HasValue) query = query.Where(r => r.SellerId == sellerId.Value);
        if (minRating.HasValue) query = query.Where(r => r.Rating >= minRating.Value);
        if (maxRating.HasValue) query = query.Where(r => r.Rating <= maxRating.Value);
        if (hasReports == true) query = query.Where(r => r.ReportCount > 0);
        if (!string.IsNullOrEmpty(search)) query = query.Where(r => r.Comment != null && r.Comment.Contains(search));

        var totalCount = await query.CountAsync();
        var reviews = await query.OrderByDescending(r => r.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(r => new {
                r.Id, r.Type, r.Rating, r.Comment, r.Status, r.ReportCount, r.HelpfulCount,
                r.IsVerifiedPurchase, r.CreatedAt,
                User = new { r.User.Id, r.User.Name },
                Product = r.Product != null ? new { r.Product.Id, r.Product.ProductName } : null
            }).ToListAsync();

        return Ok(new { Items = reviews, TotalCount = totalCount, Page = page, PageSize = pageSize });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetReview(int id)
    {
        var review = await _db.Set<Review>().Include(r => r.User).Include(r => r.Product)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (review == null) throw new NotFoundException("Review not found");
        return Ok(review);
    }

    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult> UpdateStatus(int id, [FromBody] UpdateReviewStatusDto dto)
    {
        var review = await _db.Set<Review>().FindAsync(id);
        if (review == null) throw new NotFoundException("Review not found");

        review.Status = dto.Status;
        review.ModeratedByUserId = GetCurrentUserId();
        review.ModeratedAt = DateTime.UtcNow;
        review.ModerationNote = dto.Note;

        await _db.SaveChangesAsync();
        return Ok(new { Message = "Review status updated", Status = dto.Status });
    }

    [HttpPost("{id:int}/approve")]
    public async Task<ActionResult> Approve(int id) => await UpdateStatus(id, new UpdateReviewStatusDto { Status = ReviewStatus.Approved });

    [HttpPost("{id:int}/reject")]
    public async Task<ActionResult> Reject(int id, [FromBody] RejectReviewDto dto)
        => await UpdateStatus(id, new UpdateReviewStatusDto { Status = ReviewStatus.Rejected, Note = dto.Reason });

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var review = await _db.Set<Review>().FindAsync(id);
        if (review == null) throw new NotFoundException("Review not found");
        _db.Remove(review);
        await _db.SaveChangesAsync();
        return Ok(new { Message = "Review deleted" });
    }

    [HttpGet("reports")]
    public async Task<ActionResult> GetReports([FromQuery] bool? isResolved = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var query = _db.Set<ReviewReport>().Include(r => r.Review).Include(r => r.User).AsQueryable();
        if (isResolved.HasValue) query = query.Where(r => r.IsResolved == isResolved.Value);

        var totalCount = await query.CountAsync();
        var reports = await query.OrderByDescending(r => r.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return Ok(new { Items = reports, TotalCount = totalCount, Page = page, PageSize = pageSize });
    }

    [HttpPost("reports/{id:int}/resolve")]
    public async Task<ActionResult> ResolveReport(int id)
    {
        var report = await _db.Set<ReviewReport>().FindAsync(id);
        if (report == null) throw new NotFoundException("Report not found");

        report.IsResolved = true;
        report.ResolvedByUserId = GetCurrentUserId();
        report.ResolvedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { Message = "Report resolved" });
    }

    [HttpGet("statistics")]
    public async Task<ActionResult> GetStatistics()
    {
        var reviews = await _db.Set<Review>().ToListAsync();
        return Ok(new {
            Total = reviews.Count,
            Pending = reviews.Count(r => r.Status == ReviewStatus.Pending),
            Approved = reviews.Count(r => r.Status == ReviewStatus.Approved),
            Rejected = reviews.Count(r => r.Status == ReviewStatus.Rejected),
            AverageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0,
            WithReports = reviews.Count(r => r.ReportCount > 0)
        });
    }

    private int GetCurrentUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
}

public class UpdateReviewStatusDto { public ReviewStatus Status { get; set; } public string? Note { get; set; } }
public class RejectReviewDto { public string? Reason { get; set; } }
