using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Exodus.Models.Entities;

/// <summary>
/// Satıcı hakediş/ödeme kayıtları
/// </summary>
public class SellerPayout : BaseEntity
{
    [Required]
    [StringLength(30)]
    public required string PayoutNumber { get; set; } // PAY-20260204-0001

    public int SellerId { get; set; }
    public Users Seller { get; set; } = null!;

    // Period
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }

    // Amounts
    [Column(TypeName = "decimal(18,2)")]
    public decimal GrossAmount { get; set; } // Toplam satış

    [Column(TypeName = "decimal(18,2)")]
    public decimal CommissionAmount { get; set; } // Komisyon kesintisi

    [Column(TypeName = "decimal(18,2)")]
    public decimal RefundDeductions { get; set; } // İade kesintileri

    [Column(TypeName = "decimal(18,2)")]
    public decimal OtherDeductions { get; set; } // Diğer kesintiler (ceza vs.)

    [Column(TypeName = "decimal(18,2)")]
    public decimal NetAmount { get; set; } // Net ödenecek tutar

    [StringLength(3)]
    public string Currency { get; set; } = "TRY";

    // Status
    public PayoutStatus Status { get; set; } = PayoutStatus.Pending;

    // Order details
    public int OrderCount { get; set; }
    public int ItemCount { get; set; }

    // Bank transfer info
    [StringLength(100)]
    public string? BankName { get; set; }

    [StringLength(34)]
    public string? IBAN { get; set; }

    [StringLength(100)]
    public string? AccountHolderName { get; set; }

    [StringLength(100)]
    public string? TransferReference { get; set; }

    // Dates
    public DateTime? ApprovedAt { get; set; }
    public int? ApprovedByUserId { get; set; }

    public DateTime? PaidAt { get; set; }
    public int? PaidByUserId { get; set; }

    // Notes
    [StringLength(1000)]
    public string? Notes { get; set; }

    // Related orders
    public ICollection<SellerPayoutItem> Items { get; set; } = new List<SellerPayoutItem>();
}

/// <summary>
/// Hakediş detay kalemleri
/// </summary>
public class SellerPayoutItem : BaseEntity
{
    public int PayoutId { get; set; }
    public SellerPayout Payout { get; set; } = null!;

    public int SellerOrderId { get; set; }
    public SellerOrder SellerOrder { get; set; } = null!;

    [Column(TypeName = "decimal(18,2)")]
    public decimal OrderAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal CommissionAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal NetAmount { get; set; }
}

public enum PayoutStatus
{
    Pending = 0,        // Bekliyor
    Approved = 1,       // Onaylandı
    Processing = 2,     // İşleniyor
    Paid = 3,           // Ödendi
    Failed = 4,         // Başarısız
    Cancelled = 5,      // İptal
    OnHold = 6          // Beklemede (sorun var)
}
