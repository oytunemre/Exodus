using System.ComponentModel.DataAnnotations.Schema;

namespace Exodus.Models.Entities;

public class CampaignUsage : BaseEntity
{
    public int CampaignId { get; set; }

    [ForeignKey(nameof(CampaignId))]
    public Campaign? Campaign { get; set; }

    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public Users? User { get; set; }

    public int? OrderId { get; set; }

    [ForeignKey(nameof(OrderId))]
    public Order? Order { get; set; }

    public decimal DiscountApplied { get; set; }

    public DateTime UsedAt { get; set; } = DateTime.UtcNow;
}
