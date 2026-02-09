using Exodus.Data;
using Exodus.Models.Dto;
using Exodus.Models.Entities;
using Exodus.Services.Common;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace Exodus.Services.Categories
{
    public class CategoryService : ICategoryService
    {
        private readonly ApplicationDbContext _context;

        public CategoryService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<CategoryResponseDto>> GetAllAsync(bool includeInactive = false)
        {
            var query = _context.Categories
                .Include(c => c.ParentCategory)
                .Include(c => c.Products)
                .AsQueryable();

            if (!includeInactive)
                query = query.Where(c => c.IsActive);

            var categories = await query
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();

            return categories.Select(MapToDto);
        }

        public async Task<IEnumerable<CategoryTreeDto>> GetTreeAsync()
        {
            var categories = await _context.Categories
                .Where(c => c.IsActive)
                .Include(c => c.Products)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();

            // Build tree from root categories (no parent)
            var rootCategories = categories.Where(c => c.ParentCategoryId == null);

            return rootCategories.Select(c => BuildTree(c, categories));
        }

        public async Task<CategoryResponseDto?> GetByIdAsync(int id)
        {
            var category = await _context.Categories
                .Include(c => c.ParentCategory)
                .Include(c => c.SubCategories)
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.Id == id);

            return category == null ? null : MapToDto(category);
        }

        public async Task<CategoryResponseDto?> GetBySlugAsync(string slug)
        {
            var category = await _context.Categories
                .Include(c => c.ParentCategory)
                .Include(c => c.SubCategories)
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.Slug == slug);

            return category == null ? null : MapToDto(category);
        }

        public async Task<CategoryResponseDto> CreateAsync(CreateCategoryDto dto)
        {
            // Validate parent category if specified
            if (dto.ParentCategoryId.HasValue)
            {
                var parentExists = await _context.Categories.AnyAsync(c => c.Id == dto.ParentCategoryId.Value);
                if (!parentExists)
                    throw new NotFoundException("Parent category not found");
            }

            var slug = GenerateSlug(dto.Name);

            // Ensure slug is unique
            var slugExists = await _context.Categories.AnyAsync(c => c.Slug == slug);
            if (slugExists)
                slug = $"{slug}-{DateTime.UtcNow.Ticks}";

            var category = new Category
            {
                Name = dto.Name,
                Slug = slug,
                Description = dto.Description,
                ImageUrl = dto.ImageUrl,
                ParentCategoryId = dto.ParentCategoryId,
                DisplayOrder = dto.DisplayOrder,
                IsActive = true
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return MapToDto(category);
        }

        public async Task<CategoryResponseDto> UpdateAsync(int id, UpdateCategoryDto dto)
        {
            var category = await _context.Categories
                .Include(c => c.ParentCategory)
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
                throw new NotFoundException("Category not found");

            // Prevent circular reference
            if (dto.ParentCategoryId.HasValue && dto.ParentCategoryId.Value == id)
                throw new BadRequestException("Category cannot be its own parent");

            if (dto.Name != null)
            {
                category.Name = dto.Name;
                category.Slug = GenerateSlug(dto.Name);
            }

            if (dto.Description != null)
                category.Description = dto.Description;

            if (dto.ImageUrl != null)
                category.ImageUrl = dto.ImageUrl;

            if (dto.ParentCategoryId.HasValue)
            {
                // Validate parent exists and is not a descendant
                if (dto.ParentCategoryId.Value != 0)
                {
                    var parentExists = await _context.Categories.AnyAsync(c => c.Id == dto.ParentCategoryId.Value);
                    if (!parentExists)
                        throw new NotFoundException("Parent category not found");
                }
                category.ParentCategoryId = dto.ParentCategoryId.Value == 0 ? null : dto.ParentCategoryId;
            }

            if (dto.DisplayOrder.HasValue)
                category.DisplayOrder = dto.DisplayOrder.Value;

            if (dto.IsActive.HasValue)
                category.IsActive = dto.IsActive.Value;

            await _context.SaveChangesAsync();

            return MapToDto(category);
        }

        public async Task DeleteAsync(int id)
        {
            var category = await _context.Categories
                .Include(c => c.SubCategories)
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
                throw new NotFoundException("Category not found");

            if (category.SubCategories.Any())
                throw new BadRequestException("Cannot delete category with subcategories. Delete or move subcategories first.");

            if (category.Products.Any())
                throw new BadRequestException("Cannot delete category with products. Remove products from category first.");

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<CategoryResponseDto>> GetSubCategoriesAsync(int parentId)
        {
            var categories = await _context.Categories
                .Where(c => c.ParentCategoryId == parentId && c.IsActive)
                .Include(c => c.Products)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();

            return categories.Select(MapToDto);
        }

        private static CategoryResponseDto MapToDto(Category category)
        {
            return new CategoryResponseDto
            {
                Id = category.Id,
                Name = category.Name,
                Slug = category.Slug,
                Description = category.Description,
                ImageUrl = category.ImageUrl,
                IsActive = category.IsActive,
                DisplayOrder = category.DisplayOrder,
                ParentCategoryId = category.ParentCategoryId,
                ParentCategoryName = category.ParentCategory?.Name,
                ProductCount = category.Products?.Count ?? 0,
                SubCategories = category.SubCategories?.Select(MapToDto).ToList() ?? new()
            };
        }

        private static CategoryTreeDto BuildTree(Category category, List<Category> allCategories)
        {
            var children = allCategories
                .Where(c => c.ParentCategoryId == category.Id)
                .Select(c => BuildTree(c, allCategories))
                .ToList();

            return new CategoryTreeDto
            {
                Id = category.Id,
                Name = category.Name,
                Slug = category.Slug,
                ImageUrl = category.ImageUrl,
                ProductCount = category.Products?.Count ?? 0,
                Children = children
            };
        }

        private static string GenerateSlug(string name)
        {
            var slug = name.ToLower().Trim();
            slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
            slug = Regex.Replace(slug, @"\s+", "-");
            slug = Regex.Replace(slug, @"-+", "-");
            return slug.Trim('-');
        }
    }
}
