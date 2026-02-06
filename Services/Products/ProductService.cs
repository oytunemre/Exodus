using Exodus.Data;
using Exodus.Models.Dto.ProductDto;
using Exodus.Models.Entities;
using Exodus.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace Exodus.Services.Products;

public class ProductService : IProductService
{
    private readonly ApplicationDbContext _db;

    public ProductService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<ProductResponseDto>> GetAllAsync()
    {
        var products = await _db.Products
            .Include(p => p.Barcodes)
            .ToListAsync();

        return products.Select(Map).ToList();
    }

    public async Task<ProductResponseDto> GetByIdAsync(int id)
    {
        var product = await _db.Products
            .Include(p => p.Barcodes)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product is null) throw new NotFoundException("Product not found.");

        return Map(product);
    }

    public async Task<ProductResponseDto> CreateAsync(AddProductDto dto)
    {
        var barcodes = NormalizeBarcodes(dto.Barcodes);
        if (barcodes.Count == 0) throw new BadRequestException("En az 1 barkod göndermelisin.");

        // Aktif barkod çakışması var mı?
        var alreadyUsed = await _db.ProductBarcodes
            .Where(pb => barcodes.Contains(pb.Barcode))
            .Select(pb => pb.Barcode)
            .Distinct()
            .ToListAsync();

        if (alreadyUsed.Count > 0)
            throw new ConflictException($"Bu barkod(lar) zaten kullanımda: {string.Join(", ", alreadyUsed)}");

        var entity = new Product
        {
            ProductName = dto.ProductName,
            ProductDescription = dto.ProductDescription,
            Barcodes = barcodes.Select(b => new ProductBarcode { Barcode = b }).ToList()
        };

        await _db.Products.AddAsync(entity);
        await _db.SaveChangesAsync();

        return Map(entity);
    }

    public async Task<ProductResponseDto> UpdateAsync(int id, ProductUpdateDto dto)
    {
        var product = await _db.Products
            .Include(p => p.Barcodes) // query filter: sadece IsDeleted=false barkodlar gelir
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product is null) throw new NotFoundException("Product not found.");

        var barcodes = NormalizeBarcodes(dto.Barcodes);
        if (barcodes.Count == 0) throw new BadRequestException("En az 1 barkod göndermelisin.");

        // Başka ürünlerde aktif olarak var mı?
        var usedByOthers = await _db.ProductBarcodes
            .Where(pb => barcodes.Contains(pb.Barcode) && pb.ProductId != id)
            .Select(pb => pb.Barcode)
            .Distinct()
            .ToListAsync();

        if (usedByOthers.Count > 0)
            throw new ConflictException($"Bu barkod(lar) başka üründe kullanılıyor: {string.Join(", ", usedByOthers)}");

        product.ProductName = dto.ProductName;
        product.ProductDescription = dto.ProductDescription;

        // 1) listede olmayan aktif barkodları soft delete
        var keep = barcodes.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var pb in product.Barcodes.ToList())
        {
            if (!keep.Contains(pb.Barcode))
            {
                pb.IsDeleted = true;
                pb.DeletedDate = DateTime.UtcNow;
            }
        }

        // 2) listede olup aktif barkodlarda olmayanları ekle
        var existingActive = product.Barcodes.Select(b => b.Barcode).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var toAdd = barcodes.Where(x => !existingActive.Contains(x)).ToList();

        foreach (var barcode in toAdd)
        {
            // Daha önce aynı ürün için soft delete edilmiş barcode varsa “restore” edelim (temiz iş)
            var old = await _db.ProductBarcodes
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.ProductId == id && x.Barcode == barcode);

            if (old is not null && old.IsDeleted)
            {
                old.IsDeleted = false;
                old.DeletedDate = null;
            }
            else if (old is null)
            {
                product.Barcodes.Add(new ProductBarcode { Barcode = barcode });
            }
        }

        await _db.SaveChangesAsync();

        // yeniden çek (barcodes listesi güncel)
        var updated = await _db.Products.Include(p => p.Barcodes).FirstAsync(p => p.Id == id);
        return Map(updated);
    }

    public async Task SoftDeleteAsync(int id)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (product is null) throw new NotFoundException("Product not found.");

        product.IsDeleted = true;
        product.DeletedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();
    }

    private static List<string> NormalizeBarcodes(List<string> barcodes)
    {
        return (barcodes ?? new())
            .Select(x => (x ?? "").Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static ProductResponseDto Map(Product p)
    {
        return new ProductResponseDto
        {
            Id = p.Id,
            ProductName = p.ProductName,
            ProductDescription = p.ProductDescription,
            Barcodes = p.Barcodes.Select(b => b.Barcode).ToList()
        };
    }
}
