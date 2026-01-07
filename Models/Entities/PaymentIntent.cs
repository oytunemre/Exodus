using System.ComponentModel.DataAnnotations.Schema;
using FarmazonDemo.Models.Enums;

namespace FarmazonDemo.Models.Entities;

public class PaymentIntent : BaseEntity
{
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    public string Currency { get; set; } = "TRY";

    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Created;

    public string Provider { get; set; } = "MANUAL";
    public string? ExternalReference { get; set; }

    public string? FailureReason { get; set; }
}
