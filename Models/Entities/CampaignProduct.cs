using System.ComponentModel.DataAnnotations.Schema;

namespace Exodus.Models.Entities;

public class CampaignProduct : BaseEntity
{
    public int CampaignId { get; set; }

    [ForeignKey(nameof(CampaignId))]
    public Campaign? Campaign { get; set; }

    public int? ProductId { get; set; }

    [ForeignKey(nameof(ProductId))]
    public Product? Product { get; set; }

    public int? ListingId { get; set; }

    [ForeignKey(nameof(ListingId))]
    public Listing? Listing { get; set; }
}
