using FarmazonDemo.Data;
using FarmazonDemo.Models.Dto;
using FarmazonDemo.Models.Entities;
using FarmazonDemo.Models.Enums;
using FarmazonDemo.Services.Common;
using FarmazonDemo.Services.Files;
using FarmazonDemo.Services.Profile;
using FarmazonDemo.Tests.Mocks;
using FluentAssertions;
using Moq;
using Xunit;

namespace FarmazonDemo.Tests.Unit.Services;

public class ProfileServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IFileService> _fileServiceMock;
    private readonly ProfileService _profileService;

    public ProfileServiceTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext(Guid.NewGuid().ToString());
        TestDbContextFactory.SeedTestData(_context);
        _fileServiceMock = new Mock<IFileService>();
        _profileService = new ProfileService(_context, _fileServiceMock.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region GetProfile Tests

    [Fact]
    public async Task GetProfileAsync_ExistingUser_ReturnsProfile()
    {
        // Arrange
        var userId = 1;

        // Act
        var result = await _profileService.GetProfileAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(userId);
        result.Name.Should().Be("Test User");
        result.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task GetProfileAsync_NonExistingUser_ThrowsNotFoundException()
    {
        // Arrange
        var userId = 999;

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _profileService.GetProfileAsync(userId));
    }

    #endregion

    #region UpdateProfile Tests

    [Fact]
    public async Task UpdateProfileAsync_ValidData_UpdatesProfile()
    {
        // Arrange
        var userId = 1;
        var dto = new UpdateProfileDto
        {
            Name = "Updated Name",
            Phone = "+905559876543"
        };

        // Act
        var result = await _profileService.UpdateProfileAsync(userId, dto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Updated Name");
        result.Phone.Should().Be("+905559876543");
    }

    [Fact]
    public async Task UpdateProfileAsync_DuplicateUsername_ThrowsConflictException()
    {
        // Arrange
        var userId = 1;
        var dto = new UpdateProfileDto
        {
            Username = "testseller" // This username belongs to user 2
        };

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() => _profileService.UpdateProfileAsync(userId, dto));
    }

    [Fact]
    public async Task UpdateProfileAsync_SameUsername_Succeeds()
    {
        // Arrange
        var userId = 1;
        var dto = new UpdateProfileDto
        {
            Username = "testuser" // Same username as current user
        };

        // Act
        var result = await _profileService.UpdateProfileAsync(userId, dto);

        // Assert
        result.Should().NotBeNull();
        result.Username.Should().Be("testuser");
    }

    [Fact]
    public async Task UpdateProfileAsync_NonExistingUser_ThrowsNotFoundException()
    {
        // Arrange
        var userId = 999;
        var dto = new UpdateProfileDto { Name = "Test" };

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _profileService.UpdateProfileAsync(userId, dto));
    }

    #endregion

    #region ChangePassword Tests

    [Fact]
    public async Task ChangePasswordAsync_CorrectCurrentPassword_ChangesPassword()
    {
        // Arrange
        var userId = 1;
        var dto = new ChangePasswordDto
        {
            CurrentPassword = "Test123!",
            NewPassword = "NewPassword123!"
        };

        // Act
        await _profileService.ChangePasswordAsync(userId, dto);

        // Assert
        var user = await _context.Users.FindAsync(userId);
        BCrypt.Net.BCrypt.Verify("NewPassword123!", user!.Password).Should().BeTrue();
    }

    [Fact]
    public async Task ChangePasswordAsync_IncorrectCurrentPassword_ThrowsBadRequestException()
    {
        // Arrange
        var userId = 1;
        var dto = new ChangePasswordDto
        {
            CurrentPassword = "WrongPassword",
            NewPassword = "NewPassword123!"
        };

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() => _profileService.ChangePasswordAsync(userId, dto));
    }

    [Fact]
    public async Task ChangePasswordAsync_NonExistingUser_ThrowsNotFoundException()
    {
        // Arrange
        var userId = 999;
        var dto = new ChangePasswordDto
        {
            CurrentPassword = "Test123!",
            NewPassword = "NewPassword123!"
        };

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _profileService.ChangePasswordAsync(userId, dto));
    }

    #endregion

    #region Address Tests

    [Fact]
    public async Task GetAddressesAsync_ExistingUser_ReturnsAddresses()
    {
        // Arrange
        var userId = 1;

        // Act
        var result = await _profileService.GetAddressesAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().Title.Should().Be("Home");
    }

    [Fact]
    public async Task CreateAddressAsync_ValidData_CreatesAddress()
    {
        // Arrange
        var userId = 1;
        var dto = new CreateAddressDto
        {
            Title = "Work",
            FullName = "Test User",
            Phone = "+905551234567",
            City = "Ankara",
            District = "Cankaya",
            AddressLine = "Work Address Line 1",
            IsDefault = false,
            Type = AddressType.Shipping
        };

        // Act
        var result = await _profileService.CreateAddressAsync(userId, dto);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Work");
        result.City.Should().Be("Ankara");
    }

    [Fact]
    public async Task CreateAddressAsync_SetAsDefault_UnsetsOtherDefaults()
    {
        // Arrange
        var userId = 1;
        var dto = new CreateAddressDto
        {
            Title = "Work",
            FullName = "Test User",
            Phone = "+905551234567",
            City = "Ankara",
            District = "Cankaya",
            AddressLine = "Work Address Line 1",
            IsDefault = true,
            Type = AddressType.Shipping
        };

        // Act
        var result = await _profileService.CreateAddressAsync(userId, dto);

        // Assert
        result.IsDefault.Should().BeTrue();

        // Verify old default is no longer default
        var addresses = await _profileService.GetAddressesAsync(userId);
        var oldDefault = addresses.FirstOrDefault(a => a.Title == "Home");
        oldDefault?.IsDefault.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAddressAsync_ExistingAddress_DeletesAddress()
    {
        // Arrange
        var userId = 1;
        var addressId = 1;

        // Act
        await _profileService.DeleteAddressAsync(userId, addressId);

        // Assert
        var addresses = await _profileService.GetAddressesAsync(userId);
        addresses.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteAddressAsync_NonExistingAddress_ThrowsNotFoundException()
    {
        // Arrange
        var userId = 1;
        var addressId = 999;

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _profileService.DeleteAddressAsync(userId, addressId));
    }

    [Fact]
    public async Task SetDefaultAddressAsync_ExistingAddress_SetsAsDefault()
    {
        // Arrange - First create a second address
        var userId = 1;
        var newAddress = new Address
        {
            UserId = userId,
            Title = "Office",
            FullName = "Test User",
            Phone = "+905551234567",
            City = "Izmir",
            District = "Konak",
            AddressLine = "Office Address",
            IsDefault = false,
            Type = AddressType.Shipping
        };
        _context.Addresses.Add(newAddress);
        await _context.SaveChangesAsync();

        // Act
        await _profileService.SetDefaultAddressAsync(userId, newAddress.Id);

        // Assert
        var address = await _profileService.GetAddressByIdAsync(userId, newAddress.Id);
        address.IsDefault.Should().BeTrue();

        // Old default should be unset
        var oldAddress = await _profileService.GetAddressByIdAsync(userId, 1);
        oldAddress.IsDefault.Should().BeFalse();
    }

    #endregion

    #region UserStats Tests

    [Fact]
    public async Task GetUserStatsAsync_ExistingUser_ReturnsStats()
    {
        // Arrange
        var userId = 1;

        // Act
        var result = await _profileService.GetUserStatsAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.AddressCount.Should().Be(1);
    }

    #endregion

    #region Notification Preferences Tests

    [Fact]
    public async Task GetNotificationPreferencesAsync_NewUser_CreatesDefaultPreferences()
    {
        // Arrange
        var userId = 1;

        // Act
        var result = await _profileService.GetNotificationPreferencesAsync(userId);

        // Assert
        result.Should().NotBeNull();
        // Default values
        result.EmailOrderUpdates.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateNotificationPreferencesAsync_ValidData_UpdatesPreferences()
    {
        // Arrange
        var userId = 1;
        await _profileService.GetNotificationPreferencesAsync(userId); // Create default first

        var dto = new UpdateNotificationPreferencesDto
        {
            EmailPromotions = false,
            PushPromotions = false
        };

        // Act
        var result = await _profileService.UpdateNotificationPreferencesAsync(userId, dto);

        // Assert
        result.Should().NotBeNull();
        result.EmailPromotions.Should().BeFalse();
        result.PushPromotions.Should().BeFalse();
    }

    #endregion
}
