using FarmazonDemo.Models.Enums;

namespace FarmazonDemo.Models.Dto.Payment;

public class CreatePaymentIntentDto
{
    public int OrderId { get; set; }
    public PaymentMethod Method { get; set; }
    public string Currency { get; set; } = "TRY";
}
