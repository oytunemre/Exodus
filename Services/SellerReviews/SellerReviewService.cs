using FarmazonDemo.Data;
using FarmazonDemo.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FarmazonDemo.Services.SellerReviews;

public class SellerReviewService : ISellerReviewService
{
    private readonly ApplicationDbContext _db;

    public SellerReviewService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<SellerReviewResponseDto> CreateReviewAsync(int userId, CreateSellerReviewDto dto, CancellationToken ct = default)
    {
        var seller = await _db.Users.FindAsync(new object[] { dto.SellerId }, ct)
            ?? throw new KeyNotFoundException("Satici bulunamadi");

        // Ayni satici ve siparis icin tekrar degerlendirme yapilmasin
        var existingReview = await _db.Set<SellerReview>()
            .FirstOrDefaultAsync(r => r.UserId == userId && r.SellerId == dto.SellerId && r.OrderId == dto.OrderId, ct);

        if (existingReview != null)
            throw new InvalidOperationException("Bu siparis icin zaten bir degerlendirme yapilmis");

        var review = new SellerReview
        {
            SellerId = dto.SellerId,
            UserId = userId,
            OrderId = dto.OrderId,
            Rating = dto.Rating,
            Comment = dto.Comment,
            ShippingRating = dto.ShippingRating,
            CommunicationRating = dto.CommunicationRating,
            PackagingRating = dto.PackagingRating,
            Status = SellerReviewStatus.Active
        };

        _db.Set<SellerReview>().Add(review);
        await _db.SaveChangesAsync(ct);

        return await GetReviewDtoAsync(review.Id, ct);
    }

    public async Task<SellerReviewResponseDto> UpdateReviewAsync(int userId, int reviewId, UpdateSellerReviewDto dto, CancellationToken ct = default)
    {
        var review = await _db.Set<SellerReview>()
            .FirstOrDefaultAsync(r => r.Id == reviewId && r.UserId == userId, ct)
            ?? throw new KeyNotFoundException("Degerlendirme bulunamadi");

        if (dto.Rating.HasValue) review.Rating = dto.Rating.Value;
        if (dto.Comment != null) review.Comment = dto.Comment;
        if (dto.ShippingRating.HasValue) review.ShippingRating = dto.ShippingRating.Value;
        if (dto.CommunicationRating.HasValue) review.CommunicationRating = dto.CommunicationRating.Value;
        if (dto.PackagingRating.HasValue) review.PackagingRating = dto.PackagingRating.Value;

        await _db.SaveChangesAsync(ct);
        return await GetReviewDtoAsync(reviewId, ct);
    }

    public async Task DeleteReviewAsync(int userId, int reviewId, CancellationToken ct = default)
    {
        var review = await _db.Set<SellerReview>()
            .FirstOrDefaultAsync(r => r.Id == reviewId && r.UserId == userId, ct)
            ?? throw new KeyNotFoundException("Degerlendirme bulunamadi");

        _db.Set<SellerReview>().Remove(review);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<SellerReviewResponseDto>> GetReviewsBySellerAsync(int sellerId, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var reviews = await _db.Set<SellerReview>()
            .Include(r => r.User)
            .Include(r => r.Seller)
            .Where(r => r.SellerId == sellerId && r.Status == SellerReviewStatus.Active)
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return reviews.Select(MapToDto).ToList();
    }

    public async Task<SellerReviewSummaryDto> GetSellerRatingSummaryAsync(int sellerId, CancellationToken ct = default)
    {
        var seller = await _db.Users.FindAsync(new object[] { sellerId }, ct)
            ?? throw new KeyNotFoundException("Satici bulunamadi");

        var reviews = await _db.Set<SellerReview>()
            .Where(r => r.SellerId == sellerId && r.Status == SellerReviewStatus.Active)
            .ToListAsync(ct);

        var distribution = Enumerable.Range(1, 5)
            .ToDictionary(i => i, i => reviews.Count(r => r.Rating == i));

        return new SellerReviewSummaryDto
        {
            SellerId = sellerId,
            SellerName = seller.Name,
            AverageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0,
            AverageShippingRating = reviews.Where(r => r.ShippingRating.HasValue).Select(r => (double)r.ShippingRating!.Value).DefaultIfEmpty(0).Average(),
            AverageCommunicationRating = reviews.Where(r => r.CommunicationRating.HasValue).Select(r => (double)r.CommunicationRating!.Value).DefaultIfEmpty(0).Average(),
            AveragePackagingRating = reviews.Where(r => r.PackagingRating.HasValue).Select(r => (double)r.PackagingRating!.Value).DefaultIfEmpty(0).Average(),
            TotalReviews = reviews.Count,
            RatingDistribution = distribution
        };
    }

    public async Task<SellerReviewResponseDto> ReplyToReviewAsync(int sellerId, int reviewId, string reply, CancellationToken ct = default)
    {
        var review = await _db.Set<SellerReview>()
            .FirstOrDefaultAsync(r => r.Id == reviewId && r.SellerId == sellerId, ct)
            ?? throw new KeyNotFoundException("Degerlendirme bulunamadi");

        review.SellerReply = reply;
        review.SellerReplyDate = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return await GetReviewDtoAsync(reviewId, ct);
    }

    public async Task<SellerReviewResponseDto> ReportReviewAsync(int userId, int reviewId, CancellationToken ct = default)
    {
        var review = await _db.Set<SellerReview>()
            .FirstOrDefaultAsync(r => r.Id == reviewId, ct)
            ?? throw new KeyNotFoundException("Degerlendirme bulunamadi");

        review.Status = SellerReviewStatus.Reported;
        await _db.SaveChangesAsync(ct);

        return await GetReviewDtoAsync(reviewId, ct);
    }

    public async Task<SellerReviewResponseDto> ModerateReviewAsync(int reviewId, string status, CancellationToken ct = default)
    {
        var review = await _db.Set<SellerReview>()
            .FirstOrDefaultAsync(r => r.Id == reviewId, ct)
            ?? throw new KeyNotFoundException("Degerlendirme bulunamadi");

        if (Enum.TryParse<SellerReviewStatus>(status, true, out var reviewStatus))
            review.Status = reviewStatus;

        await _db.SaveChangesAsync(ct);
        return await GetReviewDtoAsync(reviewId, ct);
    }

    private async Task<SellerReviewResponseDto> GetReviewDtoAsync(int reviewId, CancellationToken ct)
    {
        var review = await _db.Set<SellerReview>()
            .Include(r => r.User)
            .Include(r => r.Seller)
            .FirstOrDefaultAsync(r => r.Id == reviewId, ct)
            ?? throw new KeyNotFoundException("Degerlendirme bulunamadi");

        return MapToDto(review);
    }

    private static SellerReviewResponseDto MapToDto(SellerReview r)
    {
        return new SellerReviewResponseDto
        {
            Id = r.Id,
            SellerId = r.SellerId,
            SellerName = r.Seller?.Name ?? string.Empty,
            UserName = r.User?.Name ?? string.Empty,
            Rating = r.Rating,
            Comment = r.Comment,
            ShippingRating = r.ShippingRating,
            CommunicationRating = r.CommunicationRating,
            PackagingRating = r.PackagingRating,
            Status = r.Status.ToString(),
            SellerReply = r.SellerReply,
            SellerReplyDate = r.SellerReplyDate,
            CreatedAt = r.CreatedAt
        };
    }
}
