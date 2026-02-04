using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmazonDemo.Models.Entities;

/// <summary>
/// Satıcı bazlı kargo ayarları
/// Her satıcı kendi ücretsiz kargo limitini ve kargo ücretini belirleyebilir
/// </summary>
public class SellerShippingSettings : BaseEntity
{
    public int SellerId { get; set; }
    public Users Seller { get; set; } = null!;

    // Ücretsiz kargo limiti (bu tutarın üzerinde satıcı karşılar)
    [Column(TypeName = "decimal(18,2)")]
    public decimal? FreeShippingThreshold { get; set; }

    // Varsayılan kargo ücreti (ücretsiz kargo limiti altındaki siparişler için)
    [Column(TypeName = "decimal(18,2)")]
    public decimal DefaultShippingCost { get; set; } = 29.90m;

    // Tercih edilen kargo firması
    public int? PreferredCarrierId { get; set; }
    public ShippingCarrier? PreferredCarrier { get; set; }

    // Birden fazla kargo firması kullanıyor mu?
    public bool UsesMultipleCarriers { get; set; } = false;

    // Mağazadan teslim seçeneği var mı?
    public bool OffersStorePickup { get; set; } = false;

    [StringLength(500)]
    public string? PickupAddress { get; set; }

    // Aynı gün kargo seçeneği var mı?
    public bool OffersSameDayShipping { get; set; } = false;

    [Column(TypeName = "decimal(18,2)")]
    public decimal? SameDayShippingCost { get; set; }

    // Kargo süresi (iş günü)
    public int EstimatedShippingDays { get; set; } = 3;

    // İade kargo ayarları
    public bool AcceptsReturns { get; set; } = true;
    public int ReturnDaysLimit { get; set; } = 14; // Kaç gün içinde iade edilebilir

    // Satıcının ücretsiz iade sunup sunmadığı
    public bool OffersFreeReturns { get; set; } = false;

    // Aktif mi?
    public bool IsActive { get; set; } = true;
}
