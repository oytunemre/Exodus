using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Exodus.Models.Entities;

public class RecentlyViewed : BaseEntity
{
    [Required]
    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public Users User { get; set; } = null!;

    [Required]
    public int ProductId { get; set; }

    [ForeignKey(nameof(ProductId))]
    public Product Product { get; set; } = null!;

    public DateTime ViewedAt { get; set; } = DateTime.UtcNow;

    public int ViewCount { get; set; } = 1;
}
