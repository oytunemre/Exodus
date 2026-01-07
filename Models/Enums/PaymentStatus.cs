namespace FarmazonDemo.Models.Enums;

public enum PaymentStatus
{
    Created = 1,      // Intent oluşturuldu
    Captured = 2,     // Ödeme alındı (kesinleşti)
    Failed = 3,       // Ödeme başarısız
    Cancelled = 4     // İptal edildi
}
