namespace FarmazonDemo.Models.Enums
{
    public enum OrderStatus
    {
        Pending = 0,        // Sipariş oluşturuldu, ödeme bekleniyor
        Processing = 1,     // Ödeme alındı, hazırlanıyor
        Confirmed = 2,      // Satıcı onayladı
        Shipped = 3,        // Kargoya verildi
        Delivered = 4,      // Teslim edildi
        Completed = 5,      // Tamamlandı (değerlendirme yapıldı)
        Cancelled = 6,      // İptal edildi
        Refunded = 7,       // İade edildi
        PartialRefund = 8,  // Kısmi iade
        Failed = 9          // Başarısız (ödeme hatası vb.)
    }

    public enum CancellationReason
    {
        CustomerRequest,
        OutOfStock,
        PaymentFailed,
        FraudSuspected,
        SellerCancelled,
        DeliveryIssue,
        Other
    }

    public enum RefundStatus
    {
        Pending,
        Approved,
        Processing,
        Completed,
        Rejected
    }
}
