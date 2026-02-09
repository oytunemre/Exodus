using Exodus.Models.Dto;

namespace Exodus.Services.Categories
{
    public interface ICategoryService
    {
        Task<IEnumerable<CategoryResponseDto>> GetAllAsync(bool includeInactive = false);
        Task<IEnumerable<CategoryTreeDto>> GetTreeAsync();
        Task<CategoryResponseDto?> GetByIdAsync(int id);
        Task<CategoryResponseDto?> GetBySlugAsync(string slug);
        Task<CategoryResponseDto> CreateAsync(CreateCategoryDto dto);
        Task<CategoryResponseDto> UpdateAsync(int id, UpdateCategoryDto dto);
        Task DeleteAsync(int id);
        Task<IEnumerable<CategoryResponseDto>> GetSubCategoriesAsync(int parentId);
    }
}
