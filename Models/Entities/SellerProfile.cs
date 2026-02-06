using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Exodus.Models.Entities;

public class SellerProfile : BaseEntity
{
    public int UserId { get; set; }
    public Users User { get; set; } = null!;

    // Business Info
    [StringLength(200)]
    public string? BusinessName { get; set; }

    [StringLength(100)]
    public string? TaxNumber { get; set; }

    [StringLength(500)]
    public string? BusinessAddress { get; set; }

    [StringLength(20)]
    public string? BusinessPhone { get; set; }

    // Verification
    public SellerVerificationStatus VerificationStatus { get; set; } = SellerVerificationStatus.Pending;
    public DateTime? VerifiedAt { get; set; }
    public int? VerifiedByAdminId { get; set; }

    [StringLength(500)]
    public string? RejectionReason { get; set; }

    // Documents
    [StringLength(500)]
    public string? TaxDocumentUrl { get; set; }

    [StringLength(500)]
    public string? IdentityDocumentUrl { get; set; }

    [StringLength(500)]
    public string? SignatureCircularUrl { get; set; }

    // Commission
    [Column(TypeName = "decimal(5,2)")]
    public decimal? CustomCommissionRate { get; set; } // null = use default

    // Bank Account
    [StringLength(100)]
    public string? BankName { get; set; }

    [StringLength(34)]
    public string? IBAN { get; set; }

    [StringLength(100)]
    public string? AccountHolderName { get; set; }

    // Performance
    [Column(TypeName = "decimal(3,2)")]
    public decimal Rating { get; set; } = 0;
    public int RatingCount { get; set; } = 0;
    public int TotalSales { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalRevenue { get; set; } = 0;

    // Warnings/Penalties
    public int WarningCount { get; set; } = 0;
    public DateTime? SuspendedUntil { get; set; }

    [StringLength(500)]
    public string? SuspensionReason { get; set; }
}

public enum SellerVerificationStatus
{
    Pending,
    UnderReview,
    Approved,
    Rejected,
    Suspended
}
