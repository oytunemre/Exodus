using System.ComponentModel.DataAnnotations.Schema;

namespace Exodus.Models.Entities;

public class CampaignCategory : BaseEntity
{
    public int CampaignId { get; set; }

    [ForeignKey(nameof(CampaignId))]
    public Campaign? Campaign { get; set; }

    public int CategoryId { get; set; }

    [ForeignKey(nameof(CategoryId))]
    public Category? Category { get; set; }
}
