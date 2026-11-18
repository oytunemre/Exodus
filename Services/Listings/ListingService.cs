using Exodus.Data;
using Exodus.Models.Dto;
using Exodus.Models.Dto.ListingDto;
using Exodus.Models.Entities;
using Exodus.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace Exodus.Services.Listings;

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

    public async Task<ListingSearchResultDto> SearchAsync(ListingSearchDto query, CancellationToken ct = default)
    {
        if (query.MinPrice.HasValue && query.MaxPrice.HasValue && query.MinPrice > query.MaxPrice)
            throw new BadRequestException("MinPrice cannot be greater than MaxPrice.");

        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1
            ? PaginationNormalizationFilter.DefaultPageSize
            : Math.Min(query.PageSize, PaginationNormalizationFilter.MaxPageSize);

        var filtered = _db.Listings
            .AsNoTracking()
            .Where(l => l.IsActive)
            .Apply(query);

        var totalCount = await filtered.CountAsync(ct);

        var rows = await filtered
            .Sort(query.SortBy, string.Equals(query.SortOrder, "desc", StringComparison.OrdinalIgnoreCase))
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new
            {
                l.Id,
                l.ProductId,
                l.Product.ProductName,
                l.Product.ProductDescription,
                l.Product.CategoryId,
                CategoryName = l.Product.Category != null ? l.Product.Category.Name : null,
                PrimaryImageUrl = l.Product.Images.OrderBy(i => i.DisplayOrder).Select(i => i.Url).FirstOrDefault(),
                l.Price,
                l.StockQuantity,
                l.StockStatus,
                l.Condition,
                l.SellerId,
                SellerName = l.Seller.Name,
                l.IsActive,
                l.CreatedAt
            })
            .ToListAsync(ct);

        return new ListingSearchResultDto
        {
            Items = rows.Select(r => new ListingListItemDto
            {
                Id = r.Id,
                ProductId = r.ProductId,
                ProductName = r.ProductName,
                ProductDescription = Truncate(r.ProductDescription, 200),
                CategoryId = r.CategoryId,
                CategoryName = r.CategoryName,
                PrimaryImageUrl = r.PrimaryImageUrl,
                Price = r.Price,
                StockQuantity = r.StockQuantity,
                StockStatus = r.StockStatus,
                Condition = r.Condition,
                SellerId = r.SellerId,
                SellerName = r.SellerName,
                IsActive = r.IsActive,
                CreatedAt = r.CreatedAt
            }).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    private static string? Truncate(string? value, int max)
        => value is null || value.Length <= max ? value : value[..max] + "…";

    public async Task<ListingResponseDto> CreateAsync(AddListingDto dto, int callerId, bool isAdmin)
    {
        if (!isAdmin && dto.SellerId != callerId)
            throw new ForbiddenException("You can only create listings for your own seller account.");

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

    public async Task<ListingResponseDto> UpdateAsync(int id, UpdateListingDto dto, int callerId, bool isAdmin)
    {
        var listing = await _db.Listings.FirstOrDefaultAsync(l => l.Id == id);
        if (listing is null) throw new NotFoundException("Listing not found.");

        if (!isAdmin && listing.SellerId != callerId)
            throw new ForbiddenException("You can only modify your own listings.");

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

    public async Task SoftDeleteAsync(int id, int callerId, bool isAdmin)
    {
        var listing = await _db.Listings.FirstOrDefaultAsync(l => l.Id == id);
        if (listing is null) throw new NotFoundException("Listing not found.");

        if (!isAdmin && listing.SellerId != callerId)
            throw new ForbiddenException("You can only delete your own listings.");

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
