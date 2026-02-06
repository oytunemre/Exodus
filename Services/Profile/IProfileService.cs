using Exodus.Models.Dto;

namespace Exodus.Services.Profile
{
    public interface IProfileService
    {
        Task<UserProfileResponseDto> GetProfileAsync(int userId);
        Task<UserProfileResponseDto> UpdateProfileAsync(int userId, UpdateProfileDto dto);
        Task ChangePasswordAsync(int userId, ChangePasswordDto dto);
        Task<UserStatsDto> GetUserStatsAsync(int userId);
        Task<string> UploadAvatarAsync(int userId, IFormFile file);
        Task DeleteAvatarAsync(int userId);

        // Address management
        Task<IEnumerable<AddressResponseDto>> GetAddressesAsync(int userId);
        Task<AddressResponseDto> GetAddressByIdAsync(int userId, int addressId);
        Task<AddressResponseDto> CreateAddressAsync(int userId, CreateAddressDto dto);
        Task<AddressResponseDto> UpdateAddressAsync(int userId, int addressId, UpdateAddressDto dto);
        Task DeleteAddressAsync(int userId, int addressId);
        Task SetDefaultAddressAsync(int userId, int addressId);

        // Notification preferences
        Task<NotificationPreferencesDto> GetNotificationPreferencesAsync(int userId);
        Task<NotificationPreferencesDto> UpdateNotificationPreferencesAsync(int userId, UpdateNotificationPreferencesDto dto);
    }
}
