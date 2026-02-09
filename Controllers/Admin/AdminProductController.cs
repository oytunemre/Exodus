using Exodus.Data;
using Exodus.Models.Dto;
using Exodus.Services.Common;
using Exodus.Services.Files;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Exodus.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/products")]
    [Authorize(Policy = "AdminOnly")]
    public class AdminProductController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileService _fileService;

        public AdminProductController(ApplicationDbContext context, IFileService fileService)
        {
            _context = context;
            _fileService = fileService;
        }

        /// <summary>
        /// Get all products with admin details
        /// </summary>
        [HttpGet]
        public async Task<ActionResult> GetAll(
            [FromQuery] string? search,
            [FromQuery] int? categoryId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Images)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                query = query.Where(p =>
                    p.ProductName.ToLower().Contains(search) ||
                    p.ProductDescription.ToLower().Contains(search));
            }

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId);

            var totalCount = await query.CountAsync();

            var products = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new
                {
                    p.Id,
                    p.ProductName,
                    p.ProductDescription,
                    p.Brand,
                    p.CategoryId,
                    CategoryName = p.Category != null ? p.Category.Name : null,
                    PrimaryImage = p.Images.Where(i => i.IsPrimary).Select(i => i.Url).FirstOrDefault()
                        ?? p.Images.Select(i => i.Url).FirstOrDefault(),
                    ImageCount = p.Images.Count,
                    ListingCount = _context.Listings.Count(l => l.ProductId == p.Id),
                    p.CreatedAt,
                    p.UpdatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                Items = products,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            });
        }

        /// <summary>
        /// Update product category
        /// </summary>
        [HttpPatch("{id}/category")]
        public async Task<ActionResult> UpdateCategory(int id, [FromBody] UpdateProductCategoryDto dto)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                throw new NotFoundException("Product not found");

            if (dto.CategoryId.HasValue)
            {
                var categoryExists = await _context.Categories.AnyAsync(c => c.Id == dto.CategoryId);
                if (!categoryExists)
                    throw new NotFoundException("Category not found");
            }

            product.CategoryId = dto.CategoryId;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Product category updated" });
        }

        /// <summary>
        /// Upload product image
        /// </summary>
        [HttpPost("{id}/images")]
        public async Task<ActionResult<FileUploadResponseDto>> UploadImage(int id, IFormFile file)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                throw new NotFoundException("Product not found");

            var result = await _fileService.UploadProductImageAsync(file, id);
            return Ok(result);
        }

        /// <summary>
        /// Delete product image
        /// </summary>
        [HttpDelete("{id}/images/{imageId}")]
        public async Task<ActionResult> DeleteImage(int id, int imageId)
        {
            var image = await _context.ProductImages
                .FirstOrDefaultAsync(i => i.Id == imageId && i.ProductId == id);

            if (image == null)
                throw new NotFoundException("Image not found");

            await _fileService.DeleteAsync(image.Url);
            return NoContent();
        }

        /// <summary>
        /// Set primary image
        /// </summary>
        [HttpPatch("{id}/images/{imageId}/primary")]
        public async Task<ActionResult> SetPrimaryImage(int id, int imageId)
        {
            var images = await _context.ProductImages
                .Where(i => i.ProductId == id)
                .ToListAsync();

            if (!images.Any())
                throw new NotFoundException("No images found");

            var targetImage = images.FirstOrDefault(i => i.Id == imageId);
            if (targetImage == null)
                throw new NotFoundException("Image not found");

            // Reset all to non-primary
            foreach (var img in images)
                img.IsPrimary = false;

            // Set target as primary
            targetImage.IsPrimary = true;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Primary image updated" });
        }

        /// <summary>
        /// Delete product
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var product = await _context.Products
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                throw new NotFoundException("Product not found");

            // Check if there are active listings
            var hasActiveListings = await _context.Listings.AnyAsync(l => l.ProductId == id && l.IsActive);
            if (hasActiveListings)
                throw new BadRequestException("Cannot delete product with active listings");

            // Delete images from storage
            foreach (var image in product.Images)
            {
                await _fileService.DeleteAsync(image.Url);
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }

    public class UpdateProductCategoryDto
    {
        public int? CategoryId { get; set; }
    }
}
