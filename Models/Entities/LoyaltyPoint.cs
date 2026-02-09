using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Exodus.Models.Entities;

public class LoyaltyPoint : BaseEntity
{
    [Required]
    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public Users User { get; set; } = null!;

    public int TotalPoints { get; set; } = 0;

    public int AvailablePoints { get; set; } = 0;

    public int SpentPoints { get; set; } = 0;

    public int PendingPoints { get; set; } = 0;

    public LoyaltyTier Tier { get; set; } = LoyaltyTier.Bronze;

    public ICollection<LoyaltyTransaction> Transactions { get; set; } = new List<LoyaltyTransaction>();
}

public class LoyaltyTransaction : BaseEntity
{
    [Required]
    public int LoyaltyPointId { get; set; }

    [ForeignKey(nameof(LoyaltyPointId))]
    public LoyaltyPoint LoyaltyPoint { get; set; } = null!;

    public int Points { get; set; }

    public LoyaltyTransactionType Type { get; set; }

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    public int? OrderId { get; set; }

    [ForeignKey(nameof(OrderId))]
    public Order? Order { get; set; }

    [StringLength(50)]
    public string? ReferenceCode { get; set; }

    public DateTime? ExpiresAt { get; set; }
}

public enum LoyaltyTier
{
    Bronze = 0,
    Silver = 1,
    Gold = 2,
    Platinum = 3
}

public enum LoyaltyTransactionType
{
    Earned = 0,
    Spent = 1,
    Expired = 2,
    Refunded = 3,
    BonusEarned = 4,
    AdminAdjustment = 5
}
