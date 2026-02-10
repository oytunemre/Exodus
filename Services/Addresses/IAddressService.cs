using Exodus.Models.Dto;

namespace Exodus.Services.Addresses;

public interface IAddressService
{
    Task<AddressResponseDto> CreateAsync(int userId, CreateAddressDto dto, CancellationToken ct = default);
    Task<AddressResponseDto> UpdateAsync(int userId, int addressId, UpdateAddressDto dto, CancellationToken ct = default);
    Task DeleteAsync(int userId, int addressId, CancellationToken ct = default);
    Task<AddressResponseDto> GetByIdAsync(int userId, int addressId, CancellationToken ct = default);
    Task<List<AddressResponseDto>> GetAllAsync(int userId, CancellationToken ct = default);
    Task<AddressResponseDto> SetDefaultAsync(int userId, int addressId, CancellationToken ct = default);
}
