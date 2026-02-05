using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmazonDemo.Models.Entities;

/// <summary>
/// Hediye kartı/çeki
/// </summary>
public class GiftCard : BaseEntity
{
    [Required]
    [StringLength(20)]
    public required string Code { get; set; } // GFT-XXXX-XXXX-XXXX

    [Column(TypeName = "decimal(18,2)")]
    public decimal InitialBalance { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal CurrentBalance { get; set; }

    [StringLength(3)]
    public string Currency { get; set; } = "TRY";

    public GiftCardStatus Status { get; set; } = GiftCardStatus.Active;

    // Validity
    public DateTime? ExpiresAt { get; set; }

    // Purchaser info
    public int? PurchasedByUserId { get; set; }
    public Users? PurchasedBy { get; set; }

    public int? OrderId { get; set; } // If purchased via order
    public Order? Order { get; set; }

    // Recipient info
    public int? RecipientUserId { get; set; }
    public Users? Recipient { get; set; }

    [StringLength(200)]
    public string? RecipientEmail { get; set; }

    [StringLength(100)]
    public string? RecipientName { get; set; }

    [StringLength(500)]
    public string? PersonalMessage { get; set; }

    // Delivery
    public bool IsSentToRecipient { get; set; } = false;
    public DateTime? SentAt { get; set; }

    // Redemption
    public DateTime? RedeemedAt { get; set; }
    public int? RedeemedByUserId { get; set; }

    // Admin notes
    [StringLength(500)]
    public string? AdminNotes { get; set; }

    public ICollection<GiftCardUsage> Usages { get; set; } = new List<GiftCardUsage>();
}

/// <summary>
/// Hediye kartı kullanım geçmişi
/// </summary>
public class GiftCardUsage : BaseEntity
{
    public int GiftCardId { get; set; }
    public GiftCard GiftCard { get; set; } = null!;

    public int? OrderId { get; set; }
    public Order? Order { get; set; }

    public int UserId { get; set; }
    public Users User { get; set; } = null!;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal BalanceAfter { get; set; }

    public GiftCardUsageType Type { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }
}

public enum GiftCardStatus
{
    Active = 0,
    Used = 1,      // Fully used
    Expired = 2,
    Cancelled = 3,
    Suspended = 4
}

public enum GiftCardUsageType
{
    Purchase = 0,   // Used for purchase
    Refund = 1,     // Refunded back to card
    Adjustment = 2, // Manual adjustment
    Expired = 3     // Balance expired
}
