using Exodus.Data;
using Exodus.Models;
using Exodus.Models.Dto;
using Exodus.Models.Entities;
using Exodus.Models.Enums;
using Exodus.Services.Auth;
using Exodus.Services.Common;
using Exodus.Services.Email;
using Exodus.Services.TwoFactor;
using Exodus.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Exodus.Tests.Services;

public class AuthServiceTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly AuthService _service;
    private readonly Mock<IEmailService> _emailMock;
    private readonly Mock<ITwoFactorService> _twoFactorMock;
    private readonly JwtSettings _jwtSettings;

    public AuthServiceTests()
    {
        _db = TestDbContextFactory.Create();
        _emailMock = new Mock<IEmailService>();
        _twoFactorMock = new Mock<ITwoFactorService>();
        _jwtSettings = new JwtSettings
        {
            SecretKey = "ThisIsAVeryLongSecretKeyForTestingPurposes123456",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpiryMinutes = 60,
            RefreshTokenExpiryDays = 7
        };

        _service = new AuthService(
            _db,
            Options.Create(_jwtSettings),
            _emailMock.Object,
            _twoFactorMock.Object
        );
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    private async Task<Users> SeedVerifiedUserAsync(
        string name = "Test User",
        string email = "test@test.com",
        string username = "testuser",
        string password = "password123")
    {
        var user = new Users
        {
            Name = name,
            Email = email,
            Username = username,
            Password = BCrypt.Net.BCrypt.HashPassword(password),
            EmailVerified = true
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    #region RegisterAsync

    [Fact]
    public async Task RegisterAsync_WithValidData_ShouldCreateUser()
    {
        var dto = new RegisterDto
        {
            Name = "New User",
            Email = "new@test.com",
            Username = "newuser",
            Password = "password123",
            Role = UserRole.Customer
        };

        var result = await _service.RegisterAsync(dto);

        result.Should().NotBeNull();
        result.Name.Should().Be("New User");
        result.Email.Should().Be("new@test.com");
        result.Username.Should().Be("newuser");
        result.Role.Should().Be(UserRole.Customer);
        result.Token.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RegisterAsync_ShouldHashPassword()
    {
        var dto = new RegisterDto
        {
            Name = "User",
            Email = "hash@test.com",
            Username = "hashuser",
            Password = "mypassword"
        };

        await _service.RegisterAsync(dto);

        var user = _db.Users.First();
        user.Password.Should().NotBe("mypassword");
        BCrypt.Net.BCrypt.Verify("mypassword", user.Password).Should().BeTrue();
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateEmail_ShouldThrowConflictException()
    {
        await SeedVerifiedUserAsync(email: "existing@test.com");

        var dto = new RegisterDto
        {
            Name = "New",
            Email = "existing@test.com",
            Username = "newuser",
            Password = "password"
        };

        var act = () => _service.RegisterAsync(dto);
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("Email already exists");
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateUsername_ShouldThrowConflictException()
    {
        await SeedVerifiedUserAsync(username: "existinguser");

        var dto = new RegisterDto
        {
            Name = "New",
            Email = "new@test.com",
            Username = "existinguser",
            Password = "password"
        };

        var act = () => _service.RegisterAsync(dto);
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("Username already exists");
    }

    [Fact]
    public async Task RegisterAsync_ShouldSendVerificationEmail()
    {
        var dto = new RegisterDto
        {
            Name = "User",
            Email = "verify@test.com",
            Username = "verifyuser",
            Password = "password"
        };

        await _service.RegisterAsync(dto);

        _emailMock.Verify(e => e.SendEmailVerificationAsync(
            "verify@test.com", It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_ShouldSetEmailVerificationToken()
    {
        var dto = new RegisterDto
        {
            Name = "User",
            Email = "token@test.com",
            Username = "tokenuser",
            Password = "password"
        };

        await _service.RegisterAsync(dto);

        var user = _db.Users.First();
        user.EmailVerificationToken.Should().NotBeNullOrEmpty();
        user.EmailVerificationTokenExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task RegisterAsync_WithSellerRole_ShouldSetRole()
    {
        var dto = new RegisterDto
        {
            Name = "Seller",
            Email = "seller@test.com",
            Username = "selleruser",
            Password = "password",
            Role = UserRole.Seller
        };

        var result = await _service.RegisterAsync(dto);
        result.Role.Should().Be(UserRole.Seller);
    }

    [Fact]
    public async Task RegisterAsync_EmailCheckShouldBeCaseInsensitive()
    {
        await SeedVerifiedUserAsync(email: "USER@test.com");

        var dto = new RegisterDto
        {
            Name = "New",
            Email = "user@test.com",
            Username = "newuser",
            Password = "password"
        };

        var act = () => _service.RegisterAsync(dto);
        await act.Should().ThrowAsync<ConflictException>();
    }

    #endregion

    #region LoginAsync

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ShouldReturnToken()
    {
        await SeedVerifiedUserAsync(email: "login@test.com", username: "loginuser", password: "password123");

        var dto = new LoginDto
        {
            EmailOrUsername = "loginuser",
            Password = "password123"
        };

        var result = await _service.LoginAsync(dto);

        result.Token.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.Username.Should().Be("loginuser");
    }

    [Fact]
    public async Task LoginAsync_WithEmail_ShouldWork()
    {
        await SeedVerifiedUserAsync(email: "email@test.com", username: "emailuser", password: "password123");

        var dto = new LoginDto
        {
            EmailOrUsername = "email@test.com",
            Password = "password123"
        };

        var result = await _service.LoginAsync(dto);
        result.Email.Should().Be("email@test.com");
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ShouldThrow()
    {
        await SeedVerifiedUserAsync(password: "correctpass");

        var dto = new LoginDto
        {
            EmailOrUsername = "testuser",
            Password = "wrongpass"
        };

        var act = () => _service.LoginAsync(dto);
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Invalid credentials");
    }

    [Fact]
    public async Task LoginAsync_WithNonExistentUser_ShouldThrow()
    {
        var dto = new LoginDto
        {
            EmailOrUsername = "nonexistent",
            Password = "password"
        };

        var act = () => _service.LoginAsync(dto);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task LoginAsync_WithUnverifiedEmail_ShouldThrowUnauthorized()
    {
        var user = new Users
        {
            Name = "Unverified",
            Email = "unverified@test.com",
            Username = "unverified",
            Password = BCrypt.Net.BCrypt.HashPassword("password"),
            EmailVerified = false
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var dto = new LoginDto
        {
            EmailOrUsername = "unverified",
            Password = "password"
        };

        var act = () => _service.LoginAsync(dto);
        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("*verify your email*");
    }

    [Fact]
    public async Task LoginAsync_ShouldResetFailedAttemptsOnSuccess()
    {
        var user = await SeedVerifiedUserAsync(password: "password123");
        user.FailedLoginAttempts = 3;
        await _db.SaveChangesAsync();

        var dto = new LoginDto
        {
            EmailOrUsername = "testuser",
            Password = "password123"
        };

        await _service.LoginAsync(dto);

        var updated = await _db.Users.FindAsync(user.Id);
        updated!.FailedLoginAttempts.Should().Be(0);
        updated.LockoutEndTime.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_ShouldIncrementFailedAttemptsOnFailure()
    {
        await SeedVerifiedUserAsync(password: "password123");

        var dto = new LoginDto
        {
            EmailOrUsername = "testuser",
            Password = "wrongpassword"
        };

        try { await _service.LoginAsync(dto); } catch { }

        var user = _db.Users.First();
        user.FailedLoginAttempts.Should().Be(1);
    }

    [Fact]
    public async Task LoginAsync_ShouldLockAccountAfter5FailedAttempts()
    {
        var user = await SeedVerifiedUserAsync(password: "password123");
        user.FailedLoginAttempts = 4;
        await _db.SaveChangesAsync();

        var dto = new LoginDto
        {
            EmailOrUsername = "testuser",
            Password = "wrongpassword"
        };

        var act = () => _service.LoginAsync(dto);
        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("*locked*");
    }

    [Fact]
    public async Task LoginAsync_WhenAccountLocked_ShouldThrowUnauthorized()
    {
        var user = await SeedVerifiedUserAsync(password: "password123");
        user.LockoutEndTime = DateTime.UtcNow.AddMinutes(10);
        await _db.SaveChangesAsync();

        var dto = new LoginDto
        {
            EmailOrUsername = "testuser",
            Password = "password123"
        };

        var act = () => _service.LoginAsync(dto);
        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("*locked*");
    }

    [Fact]
    public async Task LoginAsync_WhenLockoutExpired_ShouldAllowLogin()
    {
        var user = await SeedVerifiedUserAsync(password: "password123");
        user.LockoutEndTime = DateTime.UtcNow.AddMinutes(-1);
        user.FailedLoginAttempts = 5;
        await _db.SaveChangesAsync();

        var dto = new LoginDto
        {
            EmailOrUsername = "testuser",
            Password = "password123"
        };

        var result = await _service.LoginAsync(dto);
        result.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task LoginAsync_With2FAEnabled_ShouldReturnTwoFactorRequired()
    {
        var user = await SeedVerifiedUserAsync(password: "password123");
        user.TwoFactorEnabled = true;
        user.TwoFactorSecretKey = "secret123";
        await _db.SaveChangesAsync();

        var dto = new LoginDto
        {
            EmailOrUsername = "testuser",
            Password = "password123"
        };

        var result = await _service.LoginAsync(dto);
        result.TwoFactorRequired.Should().BeTrue();
        result.Token.Should().BeNull();
    }

    #endregion

    #region GenerateJwtToken

    [Fact]
    public void GenerateJwtToken_ShouldReturnValidToken()
    {
        var token = _service.GenerateJwtToken(1, "testuser", "test@test.com", "Customer");

        token.Should().NotBeNullOrEmpty();
        token.Split('.').Should().HaveCount(3); // JWT format: header.payload.signature
    }

    [Fact]
    public void GenerateJwtToken_ShouldContainCorrectClaims()
    {
        var token = _service.GenerateJwtToken(1, "testuser", "test@test.com", "Admin");
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        jwt.Claims.Should().Contain(c => c.Type == "sub" && c.Value == "1");
        jwt.Claims.Should().Contain(c => c.Type == "unique_name" && c.Value == "testuser");
        jwt.Claims.Should().Contain(c => c.Type == "email" && c.Value == "test@test.com");
    }

    #endregion

    #region VerifyEmailAsync

    [Fact]
    public async Task VerifyEmailAsync_WithValidToken_ShouldVerifyEmail()
    {
        var user = new Users
        {
            Name = "User",
            Email = "verify@test.com",
            Username = "verifyuser",
            Password = "hash",
            EmailVerified = false,
            EmailVerificationToken = "valid-token",
            EmailVerificationTokenExpiresAt = DateTime.UtcNow.AddHours(1)
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var result = await _service.VerifyEmailAsync("valid-token");

        result.Should().BeTrue();
        var updated = await _db.Users.FindAsync(user.Id);
        updated!.EmailVerified.Should().BeTrue();
        updated.EmailVerificationToken.Should().BeNull();
    }

    [Fact]
    public async Task VerifyEmailAsync_WithInvalidToken_ShouldThrowNotFoundException()
    {
        var act = () => _service.VerifyEmailAsync("invalid-token");
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task VerifyEmailAsync_WithExpiredToken_ShouldThrowBadRequestException()
    {
        var user = new Users
        {
            Name = "User",
            Email = "expired@test.com",
            Username = "expireduser",
            Password = "hash",
            EmailVerified = false,
            EmailVerificationToken = "expired-token",
            EmailVerificationTokenExpiresAt = DateTime.UtcNow.AddHours(-1)
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var act = () => _service.VerifyEmailAsync("expired-token");
        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("*expired*");
    }

    [Fact]
    public async Task VerifyEmailAsync_WhenAlreadyVerified_ShouldThrowBadRequestException()
    {
        var user = new Users
        {
            Name = "User",
            Email = "already@test.com",
            Username = "alreadyuser",
            Password = "hash",
            EmailVerified = true,
            EmailVerificationToken = "already-verified-token",
            EmailVerificationTokenExpiresAt = DateTime.UtcNow.AddHours(1)
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var act = () => _service.VerifyEmailAsync("already-verified-token");
        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("*already verified*");
    }

    #endregion

    #region ForgotPasswordAsync

    [Fact]
    public async Task ForgotPasswordAsync_WithExistingEmail_ShouldSendResetEmail()
    {
        await SeedVerifiedUserAsync(email: "forgot@test.com");

        await _service.ForgotPasswordAsync("forgot@test.com");

        _emailMock.Verify(e => e.SendPasswordResetAsync(
            "forgot@test.com", It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ForgotPasswordAsync_WithNonExistentEmail_ShouldNotThrow()
    {
        // Should silently return without error for security
        var act = () => _service.ForgotPasswordAsync("nonexistent@test.com");
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ForgotPasswordAsync_ShouldSetResetToken()
    {
        var user = await SeedVerifiedUserAsync(email: "reset@test.com");

        await _service.ForgotPasswordAsync("reset@test.com");

        var updated = await _db.Users.FindAsync(user.Id);
        updated!.PasswordResetToken.Should().NotBeNullOrEmpty();
        updated.PasswordResetTokenExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    #endregion

    #region ResetPasswordAsync

    [Fact]
    public async Task ResetPasswordAsync_WithValidToken_ShouldResetPassword()
    {
        var user = await SeedVerifiedUserAsync(password: "oldpassword");
        user.PasswordResetToken = "reset-token";
        user.PasswordResetTokenExpiresAt = DateTime.UtcNow.AddHours(1);
        await _db.SaveChangesAsync();

        await _service.ResetPasswordAsync("reset-token", "newpassword123");

        var updated = await _db.Users.FindAsync(user.Id);
        BCrypt.Net.BCrypt.Verify("newpassword123", updated!.Password).Should().BeTrue();
        updated.PasswordResetToken.Should().BeNull();
        updated.PasswordResetTokenExpiresAt.Should().BeNull();
    }

    [Fact]
    public async Task ResetPasswordAsync_WithInvalidToken_ShouldThrowNotFoundException()
    {
        var act = () => _service.ResetPasswordAsync("invalid-token", "newpass");
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ResetPasswordAsync_WithExpiredToken_ShouldThrowBadRequestException()
    {
        var user = await SeedVerifiedUserAsync();
        user.PasswordResetToken = "expired-reset-token";
        user.PasswordResetTokenExpiresAt = DateTime.UtcNow.AddHours(-1);
        await _db.SaveChangesAsync();

        var act = () => _service.ResetPasswordAsync("expired-reset-token", "newpass");
        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("*expired*");
    }

    [Fact]
    public async Task ResetPasswordAsync_ShouldResetLockout()
    {
        var user = await SeedVerifiedUserAsync();
        user.PasswordResetToken = "lockout-reset-token";
        user.PasswordResetTokenExpiresAt = DateTime.UtcNow.AddHours(1);
        user.FailedLoginAttempts = 5;
        user.LockoutEndTime = DateTime.UtcNow.AddMinutes(10);
        await _db.SaveChangesAsync();

        await _service.ResetPasswordAsync("lockout-reset-token", "newpassword");

        var updated = await _db.Users.FindAsync(user.Id);
        updated!.FailedLoginAttempts.Should().Be(0);
        updated.LockoutEndTime.Should().BeNull();
    }

    #endregion

    #region RefreshTokenAsync

    [Fact]
    public async Task RefreshTokenAsync_WithValidToken_ShouldReturnNewTokens()
    {
        var user = await SeedVerifiedUserAsync();
        var refreshToken = new RefreshToken
        {
            Token = "valid-refresh-token",
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };
        _db.RefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync();

        var result = await _service.RefreshTokenAsync("valid-refresh-token");

        result.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBe("valid-refresh-token"); // New token
    }

    [Fact]
    public async Task RefreshTokenAsync_WithInvalidToken_ShouldThrowUnauthorized()
    {
        var act = () => _service.RefreshTokenAsync("invalid-token");
        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task RefreshTokenAsync_WithExpiredToken_ShouldThrowUnauthorized()
    {
        var user = await SeedVerifiedUserAsync();
        var refreshToken = new RefreshToken
        {
            Token = "expired-refresh-token",
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(-1)
        };
        _db.RefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync();

        var act = () => _service.RefreshTokenAsync("expired-refresh-token");
        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("*expired*");
    }

    [Fact]
    public async Task RefreshTokenAsync_ShouldRevokeOldToken()
    {
        var user = await SeedVerifiedUserAsync();
        var refreshToken = new RefreshToken
        {
            Token = "old-token-to-revoke",
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };
        _db.RefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync();

        await _service.RefreshTokenAsync("old-token-to-revoke");

        var oldToken = _db.RefreshTokens.First(t => t.Token == "old-token-to-revoke");
        oldToken.IsRevoked.Should().BeTrue();
        oldToken.RevokedAt.Should().NotBeNull();
    }

    #endregion

    #region RevokeRefreshTokenAsync

    [Fact]
    public async Task RevokeRefreshTokenAsync_ShouldRevokeToken()
    {
        var user = await SeedVerifiedUserAsync();
        _db.RefreshTokens.Add(new RefreshToken
        {
            Token = "token-to-revoke",
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        });
        await _db.SaveChangesAsync();

        await _service.RevokeRefreshTokenAsync("token-to-revoke");

        var token = _db.RefreshTokens.First(t => t.Token == "token-to-revoke");
        token.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_WithInvalidToken_ShouldThrowNotFoundException()
    {
        var act = () => _service.RevokeRefreshTokenAsync("nonexistent-token");
        await act.Should().ThrowAsync<NotFoundException>();
    }

    #endregion
}
