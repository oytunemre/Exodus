namespace Exodus.Models.Events;

public class PaymentSuccessEvent
{
    public int OrderId { get; set; }
    public int UserId { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "TRY";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
