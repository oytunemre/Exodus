using Exodus.Models.Dto.UserDto;

namespace Exodus.Services.Users;

public interface IUserService
{
    Task<List<UserResponseDto>> GetAllAsync();
    Task<UserResponseDto> GetByIdAsync(int id);
    Task<UserResponseDto> CreateAsync(AdduserDto dto);
    Task<UserResponseDto> UpdateAsync(int id, UserUpdateDto dto);
    Task SoftDeleteAsync(int id);
}
