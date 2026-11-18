using Exodus.Models.Dto.ListingDto;

namespace Exodus.Services.Listings;

public interface IListingService
{
    Task<List<ListingResponseDto>> GetAllAsync();
    Task<ListingResponseDto> GetByIdAsync(int id);

    Task<ListingResponseDto> CreateAsync(AddListingDto dto, int callerId, bool isAdmin);
    Task<ListingResponseDto> UpdateAsync(int id, UpdateListingDto dto, int callerId, bool isAdmin);

    Task SoftDeleteAsync(int id, int callerId, bool isAdmin);
}
