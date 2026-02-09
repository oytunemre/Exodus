namespace Exodus.Services.SellerReviews;

public interface ISellerReviewService
{
    Task<SellerReviewResponseDto> CreateReviewAsync(int userId, CreateSellerReviewDto dto, CancellationToken ct = default);
    Task<SellerReviewResponseDto> UpdateReviewAsync(int userId, int reviewId, UpdateSellerReviewDto dto, CancellationToken ct = default);
    Task DeleteReviewAsync(int userId, int reviewId, CancellationToken ct = default);
    Task<List<SellerReviewResponseDto>> GetReviewsBySellerAsync(int sellerId, int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<SellerReviewSummaryDto> GetSellerRatingSummaryAsync(int sellerId, CancellationToken ct = default);
    Task<SellerReviewResponseDto> ReplyToReviewAsync(int sellerId, int reviewId, string reply, CancellationToken ct = default);
    Task<SellerReviewResponseDto> ReportReviewAsync(int userId, int reviewId, CancellationToken ct = default);

    // Admin
    Task<SellerReviewResponseDto> ModerateReviewAsync(int reviewId, string status, CancellationToken ct = default);
}

public class CreateSellerReviewDto
{
    public int SellerId { get; set; }
    public int? OrderId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public int? ShippingRating { get; set; }
    public int? CommunicationRating { get; set; }
    public int? PackagingRating { get; set; }
}

public class UpdateSellerReviewDto
{
    public int? Rating { get; set; }
    public string? Comment { get; set; }
    public int? ShippingRating { get; set; }
    public int? CommunicationRating { get; set; }
    public int? PackagingRating { get; set; }
}

public class SellerReviewResponseDto
{
    public int Id { get; set; }
    public int SellerId { get; set; }
    public string SellerName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public int? ShippingRating { get; set; }
    public int? CommunicationRating { get; set; }
    public int? PackagingRating { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? SellerReply { get; set; }
    public DateTime? SellerReplyDate { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SellerReviewSummaryDto
{
    public int SellerId { get; set; }
    public string SellerName { get; set; } = string.Empty;
    public double AverageRating { get; set; }
    public double? AverageShippingRating { get; set; }
    public double? AverageCommunicationRating { get; set; }
    public double? AveragePackagingRating { get; set; }
    public int TotalReviews { get; set; }
    public Dictionary<int, int> RatingDistribution { get; set; } = new();
}
