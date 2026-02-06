using System.ComponentModel.DataAnnotations;

namespace Exodus.Models.Entities;

/// <summary>
/// Kullanıcı favorileri/istek listesi
/// </summary>
public class Wishlist : BaseEntity
{
    public int UserId { get; set; }
    public Users User { get; set; } = null!;

    [StringLength(100)]
    public string Name { get; set; } = "Favorilerim";

    public bool IsDefault { get; set; } = true;

    public bool IsPublic { get; set; } = false;

    public ICollection<WishlistItem> Items { get; set; } = new List<WishlistItem>();
}

/// <summary>
/// Favori ürünler
/// </summary>
public class WishlistItem : BaseEntity
{
    public int WishlistId { get; set; }
    public Wishlist Wishlist { get; set; } = null!;

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int? ListingId { get; set; }
    public Listing? Listing { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }

    // Price tracking
    public bool NotifyOnPriceDrop { get; set; } = false;

    public decimal? PriceAtAdd { get; set; }
}
