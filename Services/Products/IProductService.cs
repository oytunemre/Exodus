using Exodus.Models.Dto.ProductDto;

namespace Exodus.Services.Products;

public interface IProductService
{
    Task<List<ProductResponseDto>> GetAllAsync();
    Task<ProductResponseDto> GetByIdAsync(int id);
    Task<ProductResponseDto> CreateAsync(AddProductDto dto);
    Task<ProductResponseDto> UpdateAsync(int id, ProductUpdateDto dto);
    Task SoftDeleteAsync(int id);
}
