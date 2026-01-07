using FarmazonDemo.Models.Enums;

namespace FarmazonDemo.Models.Entities;

public class PaymentEvent : BaseEntity
{
    public int PaymentIntentId { get; set; }
    public PaymentIntent PaymentIntent { get; set; } = null!;

    public PaymentStatus Status { get; set; }

    public string? PayloadJson { get; set; }
}
