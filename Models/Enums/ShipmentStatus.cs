namespace Exodus.Models.Enums;

public enum ShipmentStatus
{
    Created = 1,    // Shipment kaydı açıldı
    Packed = 2,     // Paket hazır
    Shipped = 3,    // Kargoya verildi
    Delivered = 4,  // Teslim edildi
    Returned = 5,   // İade döndü
    Cancelled = 6   // İptal edildi
}
