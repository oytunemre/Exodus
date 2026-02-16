using Exodus.Models.Dto.UserDto;
using Exodus.Models.Entities;
using Exodus.Services.Common;
using Exodus.Services.Users;
using Exodus.Tests.Helpers;
using FluentAssertions;
using Xunit;

namespace Exodus.Tests.Services;

public class UserServiceTests : IDisposable
{
    private readonly Data.ApplicationDbContext _db;
    private readonly UserService _service;

    public UserServiceTests()
    {
        _db = TestDbContextFactory.Create();
        _service = new UserService(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    private async Task<Users> SeedUserAsync(string name = "Test User", string email = "test@test.com", string username = "testuser")
    {
        var user = new Users
        {
            Name = name,
            Email = email,
            Password = "hashedpass",
            Username = username
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    #region GetAllAsync

    [Fact]
    public async Task GetAllAsync_WhenNoUsers_ShouldReturnEmptyList()
    {
        var result = await _service.GetAllAsync();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_WhenUsersExist_ShouldReturnAll()
    {
        await SeedUserAsync("User 1", "user1@test.com", "user1");
        await SeedUserAsync("User 2", "user2@test.com", "user2");

        var result = await _service.GetAllAsync();
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllAsync_ShouldMapCorrectly()
    {
        await SeedUserAsync("John Doe", "john@test.com", "johndoe");

        var result = await _service.GetAllAsync();
        result[0].Name.Should().Be("John Doe");
        result[0].Email.Should().Be("john@test.com");
        result[0].Username.Should().Be("johndoe");
    }

    #endregion

    #region GetByIdAsync

    [Fact]
    public async Task GetByIdAsync_WhenExists_ShouldReturnUser()
    {
        var user = await SeedUserAsync();

        var result = await _service.GetByIdAsync(user.Id);
        result.Should().NotBeNull();
        result.Name.Should().Be("Test User");
        result.Email.Should().Be("test@test.com");
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotExists_ShouldThrowNotFoundException()
    {
        var act = () => _service.GetByIdAsync(999);
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("User not found.");
    }

    #endregion

    #region CreateAsync

    [Fact]
    public async Task CreateAsync_WithValidData_ShouldCreateUser()
    {
        var dto = new AdduserDto
        {
            Name = "New User",
            Email = "new@test.com",
            Password = "password123",
            Username = "newuser"
        };

        var result = await _service.CreateAsync(dto);

        result.Should().NotBeNull();
        result.Name.Should().Be("New User");
        result.Email.Should().Be("new@test.com");
        result.Username.Should().Be("newuser");
        result.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateEmail_ShouldThrowConflictException()
    {
        await SeedUserAsync("Existing", "existing@test.com", "existing");

        var dto = new AdduserDto
        {
            Name = "New User",
            Email = "existing@test.com",
            Password = "password123",
            Username = "newuser"
        };

        var act = () => _service.CreateAsync(dto);
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("Email already used.");
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateUsername_ShouldThrowConflictException()
    {
        await SeedUserAsync("Existing", "existing@test.com", "existing");

        var dto = new AdduserDto
        {
            Name = "New User",
            Email = "new@test.com",
            Password = "password123",
            Username = "existing"
        };

        var act = () => _service.CreateAsync(dto);
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("Username already used.");
    }

    [Fact]
    public async Task CreateAsync_ShouldPersistToDatabase()
    {
        var dto = new AdduserDto
        {
            Name = "Persist User",
            Email = "persist@test.com",
            Password = "password",
            Username = "persist"
        };

        await _service.CreateAsync(dto);

        var dbUser = await _db.Users.FindAsync(1);
        dbUser.Should().NotBeNull();
        dbUser!.Name.Should().Be("Persist User");
    }

    #endregion

    #region UpdateAsync

    [Fact]
    public async Task UpdateAsync_WithValidData_ShouldUpdateUser()
    {
        var user = await SeedUserAsync();

        var dto = new UserUpdateDto
        {
            Name = "Updated Name",
            Email = "updated@test.com",
            Password = "newpass",
            Username = "updateduser"
        };

        var result = await _service.UpdateAsync(user.Id, dto);

        result.Name.Should().Be("Updated Name");
        result.Email.Should().Be("updated@test.com");
        result.Username.Should().Be("updateduser");
    }

    [Fact]
    public async Task UpdateAsync_WhenNotExists_ShouldThrowNotFoundException()
    {
        var dto = new UserUpdateDto
        {
            Name = "Test",
            Email = "test@test.com",
            Password = "pass",
            Username = "test"
        };

        var act = () => _service.UpdateAsync(999, dto);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_WithDuplicateEmail_ShouldThrowConflictException()
    {
        var user1 = await SeedUserAsync("User1", "user1@test.com", "user1");
        var user2 = await SeedUserAsync("User2", "user2@test.com", "user2");

        var dto = new UserUpdateDto
        {
            Name = "Updated",
            Email = "user1@test.com",
            Password = "pass",
            Username = "user2updated"
        };

        var act = () => _service.UpdateAsync(user2.Id, dto);
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("Email already used.");
    }

    [Fact]
    public async Task UpdateAsync_WithDuplicateUsername_ShouldThrowConflictException()
    {
        var user1 = await SeedUserAsync("User1", "user1@test.com", "user1");
        var user2 = await SeedUserAsync("User2", "user2@test.com", "user2");

        var dto = new UserUpdateDto
        {
            Name = "Updated",
            Email = "user2updated@test.com",
            Password = "pass",
            Username = "user1"
        };

        var act = () => _service.UpdateAsync(user2.Id, dto);
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("Username already used.");
    }

    [Fact]
    public async Task UpdateAsync_WithSameEmail_ShouldNotConflict()
    {
        var user = await SeedUserAsync("User1", "user1@test.com", "user1");

        var dto = new UserUpdateDto
        {
            Name = "Updated",
            Email = "user1@test.com",
            Password = "pass",
            Username = "user1"
        };

        var result = await _service.UpdateAsync(user.Id, dto);
        result.Name.Should().Be("Updated");
    }

    #endregion

    #region SoftDeleteAsync

    [Fact]
    public async Task SoftDeleteAsync_WhenExists_ShouldMarkAsDeleted()
    {
        var user = await SeedUserAsync();

        await _service.SoftDeleteAsync(user.Id);

        var dbUser = _db.Users.IgnoreQueryFilters().First(u => u.Id == user.Id);
        dbUser.IsDeleted.Should().BeTrue();
        dbUser.DeletedDate.Should().NotBeNull();
    }

    [Fact]
    public async Task SoftDeleteAsync_WhenNotExists_ShouldThrowNotFoundException()
    {
        var act = () => _service.SoftDeleteAsync(999);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    #endregion
}
