using Exodus.Models.Dto.ListingDto;

namespace Exodus.Services.Listings;

public interface IListingService
{
    Task<List<ListingResponseDto>> GetAllAsync();
    Task<ListingResponseDto> GetByIdAsync(int id);

    Task<ListingResponseDto> CreateAsync(AddListingDto dto);   // <-- CreateListingDto değil
    Task<ListingResponseDto> UpdateAsync(int id, UpdateListingDto dto);

    Task SoftDeleteAsync(int id);
}
