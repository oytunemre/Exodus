using Exodus.Data;
using Exodus.Models.Dto.UserDto;
using Exodus.Services.Common;
using Microsoft.EntityFrameworkCore;

// ALIAS: Entity'yi farklı isimle çağırıyoruz
using UserEntity = Exodus.Models.Entities.Users;

namespace Exodus.Services.Users;

public class UserService : IUserService
{
    private readonly ApplicationDbContext _db;

    public UserService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<UserResponseDto>> GetAllAsync()
    {
        var users = await _db.Users.ToListAsync();
        return users.Select(Map).ToList();
    }

    public async Task<UserResponseDto> GetByIdAsync(int id)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) throw new NotFoundException("User not found.");
        return Map(user);
    }

    public async Task<UserResponseDto> CreateAsync(AdduserDto dto)
    {
        var emailUsed = await _db.Users.AnyAsync(u => u.Email == dto.Email);
        if (emailUsed) throw new ConflictException("Email already used.");

        var usernameUsed = await _db.Users.AnyAsync(u => u.Username == dto.Username);
        if (usernameUsed) throw new ConflictException("Username already used.");

        var entity = new UserEntity
        {
            Name = dto.Name,
            Email = dto.Email,
            Password = dto.Password,
            Username = dto.Username
        };

        await _db.Users.AddAsync(entity);
        await _db.SaveChangesAsync();

        return Map(entity);
    }

    public async Task<UserResponseDto> UpdateAsync(int id, UserUpdateDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) throw new NotFoundException("User not found.");

        var emailUsed = await _db.Users.AnyAsync(u => u.Email == dto.Email && u.Id != id);
        if (emailUsed) throw new ConflictException("Email already used.");

        var usernameUsed = await _db.Users.AnyAsync(u => u.Username == dto.Username && u.Id != id);
        if (usernameUsed) throw new ConflictException("Username already used.");

        user.Name = dto.Name;
        user.Email = dto.Email;
        user.Password = dto.Password;
        user.Username = dto.Username;

        await _db.SaveChangesAsync();
        return Map(user);
    }

    public async Task SoftDeleteAsync(int id)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) throw new NotFoundException("User not found.");

        user.IsDeleted = true;
        user.DeletedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();
    }

    private static UserResponseDto Map(UserEntity u)
    {
        return new UserResponseDto
        {
            Id = u.Id,
            Name = u.Name,
            Email = u.Email,
            Username = u.Username
        };
    }
}
