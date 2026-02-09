using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Exodus.Models.Entities;

public class SellerReview : BaseEntity
{
    [Required]
    public int SellerId { get; set; }

    [ForeignKey(nameof(SellerId))]
    public Users Seller { get; set; } = null!;

    [Required]
    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public Users User { get; set; } = null!;

    public int? OrderId { get; set; }

    [ForeignKey(nameof(OrderId))]
    public Order? Order { get; set; }

    [Range(1, 5)]
    public int Rating { get; set; }

    [StringLength(1000)]
    public string? Comment { get; set; }

    // Alt puanlar
    [Range(1, 5)]
    public int? ShippingRating { get; set; }

    [Range(1, 5)]
    public int? CommunicationRating { get; set; }

    [Range(1, 5)]
    public int? PackagingRating { get; set; }

    public SellerReviewStatus Status { get; set; } = SellerReviewStatus.Active;

    [StringLength(1000)]
    public string? SellerReply { get; set; }

    public DateTime? SellerReplyDate { get; set; }
}

public enum SellerReviewStatus
{
    Active = 0,
    Hidden = 1,
    Reported = 2,
    Removed = 3
}
