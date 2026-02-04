using FarmazonDemo.Data;
using FarmazonDemo.Models.Dto.ListingDto;
using FarmazonDemo.Models.Entities;
using FarmazonDemo.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace FarmazonDemo.Services.Listings;

public class ListingService : IListingService
{
    private readonly ApplicationDbContext _db;

    public ListingService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<ListingResponseDto>> GetAllAsync()
    {
        var listings = await _db.Listings.ToListAsync();
        return listings.Select(Map).ToList();
    }

    public async Task<ListingResponseDto> GetByIdAsync(int id)
    {
        var listing = await _db.Listings.FirstOrDefaultAsync(l => l.Id == id);
        if (listing is null) throw new NotFoundException("Listing not found.");
        return Map(listing);
    }

    public async Task<ListingResponseDto> CreateAsync(AddListingDto dto)
    {
        var productExists = await _db.Products.AnyAsync(p => p.Id == dto.ProductId);
        if (!productExists) throw new BadRequestException("Product not found.");

        var sellerExists = await _db.Users.AnyAsync(u => u.Id == dto.SellerId);
        if (!sellerExists) throw new BadRequestException("Seller not found.");

        if (dto.Price <= 0) throw new BadRequestException("Price must be > 0.");
        if (dto.Stock < 0) throw new BadRequestException("Stock must be >= 0.");

        var entity = new Listing
        {
            ProductId = dto.ProductId,
            SellerId = dto.SellerId,
            Price = dto.Price,
            StockQuantity = dto.Stock,
            Condition = dto.Condition, // enum
            IsActive = true
        };

        await _db.Listings.AddAsync(entity);
        await _db.SaveChangesAsync();

        return Map(entity);
    }

    public async Task<ListingResponseDto> UpdateAsync(int id, UpdateListingDto dto)
    {
        var listing = await _db.Listings.FirstOrDefaultAsync(l => l.Id == id);
        if (listing is null) throw new NotFoundException("Listing not found.");

        if (dto.Price <= 0) throw new BadRequestException("Price must be > 0.");
        if (dto.Stock < 0) throw new BadRequestException("Stock must be >= 0.");

        listing.Price = dto.Price;
        listing.StockQuantity = dto.Stock;

        // dto.Condition nullable; gelmediyse mevcut kalsın
        if (dto.Condition.HasValue)
            listing.Condition = dto.Condition.Value;

        listing.IsActive = dto.IsActive;

        await _db.SaveChangesAsync();
        return Map(listing);
    }

    public async Task SoftDeleteAsync(int id)
    {
        var listing = await _db.Listings.FirstOrDefaultAsync(l => l.Id == id);
        if (listing is null) throw new NotFoundException("Listing not found.");

        listing.IsDeleted = true;
        listing.DeletedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();
    }

    private static ListingResponseDto Map(Listing l)
    {
        return new ListingResponseDto
        {
            Id = l.Id,
            ProductId = l.ProductId,
            SellerId = l.SellerId,
            Price = l.Price,
            Stock = l.StockQuantity,
            Condition = l.Condition, // enum
            IsActive = l.IsActive
        };
    }
}
