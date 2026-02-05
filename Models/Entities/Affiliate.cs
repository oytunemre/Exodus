using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmazonDemo.Models.Entities;

/// <summary>
/// Affiliate/Referral programı
/// </summary>
public class Affiliate : BaseEntity
{
    public int UserId { get; set; }
    public Users User { get; set; } = null!;

    [Required]
    [StringLength(20)]
    public required string ReferralCode { get; set; } // REF-XXXXX

    // Commission settings
    [Column(TypeName = "decimal(5,2)")]
    public decimal CommissionRate { get; set; } = 5; // Default %5

    [Column(TypeName = "decimal(18,2)")]
    public decimal MinPayoutAmount { get; set; } = 100;

    // Statistics
    public int TotalReferrals { get; set; } = 0;
    public int SuccessfulReferrals { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalEarnings { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal PendingEarnings { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal PaidEarnings { get; set; } = 0;

    // Status
    public AffiliateStatus Status { get; set; } = AffiliateStatus.Pending;
    public DateTime? ApprovedAt { get; set; }
    public int? ApprovedByUserId { get; set; }

    // Payment info
    [StringLength(100)]
    public string? BankName { get; set; }

    [StringLength(34)]
    public string? IBAN { get; set; }

    [StringLength(100)]
    public string? AccountHolderName { get; set; }

    public ICollection<AffiliateReferral> Referrals { get; set; } = new List<AffiliateReferral>();
    public ICollection<AffiliatePayout> Payouts { get; set; } = new List<AffiliatePayout>();
}

/// <summary>
/// Referral kayıtları
/// </summary>
public class AffiliateReferral : BaseEntity
{
    public int AffiliateId { get; set; }
    public Affiliate Affiliate { get; set; } = null!;

    public int ReferredUserId { get; set; }
    public Users ReferredUser { get; set; } = null!;

    public int? OrderId { get; set; }
    public Order? Order { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal OrderAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal CommissionAmount { get; set; }

    public AffiliateReferralStatus Status { get; set; } = AffiliateReferralStatus.Pending;

    // Tracking
    [StringLength(500)]
    public string? ReferralUrl { get; set; }

    [StringLength(100)]
    public string? UtmSource { get; set; }

    [StringLength(100)]
    public string? UtmMedium { get; set; }

    [StringLength(100)]
    public string? UtmCampaign { get; set; }
}

/// <summary>
/// Affiliate ödemeleri
/// </summary>
public class AffiliatePayout : BaseEntity
{
    public int AffiliateId { get; set; }
    public Affiliate Affiliate { get; set; } = null!;

    [Required]
    [StringLength(30)]
    public required string PayoutNumber { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [StringLength(3)]
    public string Currency { get; set; } = "TRY";

    public PayoutStatus Status { get; set; } = PayoutStatus.Pending;

    [StringLength(100)]
    public string? TransferReference { get; set; }

    public DateTime? PaidAt { get; set; }
    public int? PaidByUserId { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }
}

public enum AffiliateStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Suspended = 3
}

public enum AffiliateReferralStatus
{
    Pending = 0,    // Kayıt oldu ama sipariş vermedi
    Qualified = 1,  // Sipariş verdi
    Approved = 2,   // Komisyon onaylandı
    Paid = 3,       // Ödendi
    Cancelled = 4   // İptal (iade vs.)
}
