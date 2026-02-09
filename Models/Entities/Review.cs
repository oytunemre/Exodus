using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Exodus.Models.Entities;

/// <summary>
/// Ürün ve satıcı değerlendirmeleri
/// </summary>
public class Review : BaseEntity
{
    public int UserId { get; set; }
    public Users User { get; set; } = null!;

    public int? ProductId { get; set; }
    public Product? Product { get; set; }

    public int? SellerId { get; set; }
    public Users? Seller { get; set; }

    public int? OrderId { get; set; }
    public Order? Order { get; set; }

    public int? SellerOrderId { get; set; }
    public SellerOrder? SellerOrder { get; set; }

    public ReviewType Type { get; set; }

    [Range(1, 5)]
    public int Rating { get; set; }

    [StringLength(2000)]
    public string? Comment { get; set; }

    // Pros and cons
    [StringLength(500)]
    public string? Pros { get; set; }

    [StringLength(500)]
    public string? Cons { get; set; }

    // Images
    [StringLength(2000)]
    public string? ImageUrls { get; set; } // Comma-separated

    // Moderation
    public ReviewStatus Status { get; set; } = ReviewStatus.Pending;
    public int? ModeratedByUserId { get; set; }
    public DateTime? ModeratedAt { get; set; }

    [StringLength(500)]
    public string? ModerationNote { get; set; }

    // Helpfulness
    public int HelpfulCount { get; set; } = 0;
    public int NotHelpfulCount { get; set; } = 0;

    // Reports
    public int ReportCount { get; set; } = 0;

    // Verified purchase
    public bool IsVerifiedPurchase { get; set; } = false;

    // Seller response
    [StringLength(1000)]
    public string? SellerResponse { get; set; }
    public DateTime? SellerRespondedAt { get; set; }
}

public enum ReviewType
{
    Product = 0,
    Seller = 1
}

public enum ReviewStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Flagged = 3
}

/// <summary>
/// Review helpfulness votes
/// </summary>
public class ReviewVote : BaseEntity
{
    public int ReviewId { get; set; }
    public Review Review { get; set; } = null!;

    public int UserId { get; set; }
    public Users User { get; set; } = null!;

    public bool IsHelpful { get; set; }
}

/// <summary>
/// Review reports
/// </summary>
public class ReviewReport : BaseEntity
{
    public int ReviewId { get; set; }
    public Review Review { get; set; } = null!;

    public int UserId { get; set; }
    public Users User { get; set; } = null!;

    public ReviewReportReason Reason { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    public bool IsResolved { get; set; } = false;
    public int? ResolvedByUserId { get; set; }
    public DateTime? ResolvedAt { get; set; }
}

public enum ReviewReportReason
{
    Spam = 0,
    Inappropriate = 1,
    FakeReview = 2,
    NotRelevant = 3,
    Harassment = 4,
    Other = 5
}
