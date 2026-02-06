using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Exodus.Models.Entities;

/// <summary>
/// İade kargo bilgileri - Ticket veya Refund ile ilişkili
/// </summary>
public class ReturnShipment : BaseEntity
{
    [Required]
    [StringLength(30)]
    public required string ReturnCode { get; set; } // İade kargo kodu (örn: RET-20260204-0001)

    // İlişkili ticket veya refund
    public int? TicketId { get; set; }
    public SupportTicket? Ticket { get; set; }

    public int? RefundId { get; set; }
    public Refund? Refund { get; set; }

    // Sipariş bilgileri
    public int SellerOrderId { get; set; }
    public SellerOrder SellerOrder { get; set; } = null!;

    // Kargo firması
    public int? CarrierId { get; set; }
    public ShippingCarrier? Carrier { get; set; }

    [StringLength(50)]
    public string? CarrierName { get; set; } // Carrier seçilmediyse manuel girilebilir

    [StringLength(100)]
    public string? TrackingNumber { get; set; }

    // İade nedeni kategorisi
    public ReturnReason Reason { get; set; }

    [StringLength(500)]
    public string? ReasonDescription { get; set; }

    // Kargo maliyeti
    [Column(TypeName = "decimal(18,2)")]
    public decimal ShippingCost { get; set; } = 0;

    public ShippingPaidBy PaidBy { get; set; }

    // Durum
    public ReturnShipmentStatus Status { get; set; } = ReturnShipmentStatus.Pending;

    // Tarihler
    public DateTime? CodeGeneratedAt { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public DateTime? ExpiresAt { get; set; } // Kargo kodu geçerlilik süresi

    // Alıcı tarafından mı satıcı tarafından mı gönderilecek
    public bool IsPickupRequested { get; set; } = false; // Kargo gelip alsın mı?

    [StringLength(500)]
    public string? PickupAddress { get; set; }

    // Admin notları
    [StringLength(1000)]
    public string? AdminNotes { get; set; }
}

/// <summary>
/// İade nedeni - Kargo maliyetini belirler
/// </summary>
public enum ReturnReason
{
    // Alıcı kaynaklı (Alıcı öder)
    CustomerChangedMind = 0,    // Müşteri vazgeçti
    WrongSize = 1,              // Yanlış beden seçtim
    NotAsExpected = 2,          // Beklediğim gibi değil
    FoundCheaper = 3,           // Başka yerde daha ucuz buldum
    NoLongerNeeded = 4,         // Artık ihtiyacım yok

    // Satıcı kaynaklı (Satıcı öder)
    DamagedProduct = 10,        // Hasarlı ürün
    DefectiveProduct = 11,      // Arızalı/bozuk ürün
    WrongProductSent = 12,      // Yanlış ürün gönderildi
    MissingParts = 13,          // Eksik parça
    ExpiredProduct = 14,        // Son kullanma tarihi geçmiş
    FakeProduct = 15,           // Sahte/taklit ürün
    NotAsDescribed = 16,        // Açıklamadan farklı ürün

    // Platform kaynaklı (Platform öder) - Nadir durumlar
    ShippingDamage = 20,        // Kargo hasarı (kargo firması kaynaklı)
    LostInTransit = 21,         // Kargoda kayboldu

    Other = 99                  // Diğer
}

/// <summary>
/// Kargo maliyetini kim ödeyecek
/// </summary>
public enum ShippingPaidBy
{
    Buyer = 0,      // Alıcı
    Seller = 1,     // Satıcı
    Platform = 2    // Platform (Exodus)
}

/// <summary>
/// İade kargo durumu
/// </summary>
public enum ReturnShipmentStatus
{
    Pending = 0,            // Kod oluşturuldu, bekliyor
    CodeGenerated = 1,      // Kargo kodu oluşturuldu
    PickupScheduled = 2,    // Kargo alma randevusu alındı
    InTransit = 3,          // Kargo yolda
    Delivered = 4,          // Satıcıya ulaştı
    Inspecting = 5,         // İnceleniyor
    Approved = 6,           // Onaylandı
    Rejected = 7,           // Reddedildi (ürün uygun değil)
    Expired = 8,            // Süre doldu
    Cancelled = 9           // İptal edildi
}

/// <summary>
/// İade nedenine göre kargo maliyetini kimin ödeyeceğini belirler
/// </summary>
public static class ReturnReasonExtensions
{
    public static ShippingPaidBy GetPaidBy(this ReturnReason reason)
    {
        return reason switch
        {
            // Alıcı kaynaklı - Alıcı öder
            ReturnReason.CustomerChangedMind => ShippingPaidBy.Buyer,
            ReturnReason.WrongSize => ShippingPaidBy.Buyer,
            ReturnReason.NotAsExpected => ShippingPaidBy.Buyer,
            ReturnReason.FoundCheaper => ShippingPaidBy.Buyer,
            ReturnReason.NoLongerNeeded => ShippingPaidBy.Buyer,

            // Satıcı kaynaklı - Satıcı öder
            ReturnReason.DamagedProduct => ShippingPaidBy.Seller,
            ReturnReason.DefectiveProduct => ShippingPaidBy.Seller,
            ReturnReason.WrongProductSent => ShippingPaidBy.Seller,
            ReturnReason.MissingParts => ShippingPaidBy.Seller,
            ReturnReason.ExpiredProduct => ShippingPaidBy.Seller,
            ReturnReason.FakeProduct => ShippingPaidBy.Seller,
            ReturnReason.NotAsDescribed => ShippingPaidBy.Seller,

            // Platform kaynaklı - Platform öder
            ReturnReason.ShippingDamage => ShippingPaidBy.Platform,
            ReturnReason.LostInTransit => ShippingPaidBy.Platform,

            // Diğer - Varsayılan olarak alıcı öder (admin değiştirebilir)
            _ => ShippingPaidBy.Buyer
        };
    }

    public static bool IsBuyerFault(this ReturnReason reason)
    {
        return reason switch
        {
            ReturnReason.CustomerChangedMind => true,
            ReturnReason.WrongSize => true,
            ReturnReason.NotAsExpected => true,
            ReturnReason.FoundCheaper => true,
            ReturnReason.NoLongerNeeded => true,
            _ => false
        };
    }

    public static bool IsSellerFault(this ReturnReason reason)
    {
        return reason switch
        {
            ReturnReason.DamagedProduct => true,
            ReturnReason.DefectiveProduct => true,
            ReturnReason.WrongProductSent => true,
            ReturnReason.MissingParts => true,
            ReturnReason.ExpiredProduct => true,
            ReturnReason.FakeProduct => true,
            ReturnReason.NotAsDescribed => true,
            _ => false
        };
    }
}
