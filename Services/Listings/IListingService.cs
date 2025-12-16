using FarmazonDemo.Models.Dto.ListingDto;

namespace FarmazonDemo.Services.Listings;

public interface IListingService
{
    Task<List<ListingResponseDto>> GetAllAsync();
    Task<ListingResponseDto> GetByIdAsync(int id);

    Task<ListingResponseDto> CreateAsync(AddListingDto dto);   // <-- CreateListingDto değil
    Task<ListingResponseDto> UpdateAsync(int id, UpdateListingDto dto);

    Task SoftDeleteAsync(int id);
}
