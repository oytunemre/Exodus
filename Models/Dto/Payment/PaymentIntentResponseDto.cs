using FarmazonDemo.Models.Enums;

namespace FarmazonDemo.Models.Dto.Payment;

public class PaymentIntentResponseDto
{
    public int Id { get; set; }
    public int OrderId { get; set; }

    public decimal Amount { get; set; }
    public string Currency { get; set; } = "TRY";

    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; }

    public string Provider { get; set; } = "MANUAL";
    public string? ExternalReference { get; set; }
    public string? FailureReason { get; set; }
}
