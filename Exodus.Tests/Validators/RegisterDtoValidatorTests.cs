using Exodus.Models.Dto;
using Exodus.Models.Enums;
using Exodus.Validation;
using FluentAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace Exodus.Tests.Validators;

public class RegisterDtoValidatorTests
{
    private readonly RegisterDtoValidator _validator = new();

    private static RegisterDto ValidDto() => new()
    {
        Name = "Test User",
        Email = "test@example.com",
        Username = "testuser",
        Password = "password123",
        Role = UserRole.Customer
    };

    #region Name Validation

    [Fact]
    public void Name_WhenEmpty_ShouldHaveError()
    {
        var dto = ValidDto();
        dto.Name = "";
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Name_WhenTooShort_ShouldHaveError()
    {
        var dto = ValidDto();
        dto.Name = "A";
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Name must be at least 2 characters");
    }

    [Fact]
    public void Name_WhenTooLong_ShouldHaveError()
    {
        var dto = ValidDto();
        dto.Name = new string('A', 101);
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Name cannot exceed 100 characters");
    }

    [Fact]
    public void Name_WhenValid_ShouldNotHaveError()
    {
        var dto = ValidDto();
        dto.Name = "John Doe";
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    #endregion

    #region Email Validation

    [Fact]
    public void Email_WhenEmpty_ShouldHaveError()
    {
        var dto = ValidDto();
        dto.Email = "";
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Email_WhenInvalidFormat_ShouldHaveError()
    {
        var dto = ValidDto();
        dto.Email = "invalid-email";
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Invalid email format");
    }

    [Fact]
    public void Email_WhenTooLong_ShouldHaveError()
    {
        var dto = ValidDto();
        dto.Email = new string('a', 90) + "@test.com";
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Email_WhenValid_ShouldNotHaveError()
    {
        var dto = ValidDto();
        dto.Email = "user@example.com";
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Email);
    }

    #endregion

    #region Username Validation

    [Fact]
    public void Username_WhenEmpty_ShouldHaveError()
    {
        var dto = ValidDto();
        dto.Username = "";
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Username);
    }

    [Fact]
    public void Username_WhenTooShort_ShouldHaveError()
    {
        var dto = ValidDto();
        dto.Username = "ab";
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Username)
            .WithErrorMessage("Username must be at least 3 characters");
    }

    [Fact]
    public void Username_WhenTooLong_ShouldHaveError()
    {
        var dto = ValidDto();
        dto.Username = new string('a', 51);
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Username);
    }

    [Fact]
    public void Username_WhenContainsSpecialChars_ShouldHaveError()
    {
        var dto = ValidDto();
        dto.Username = "user@name!";
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Username)
            .WithErrorMessage("Username can only contain letters, numbers and underscores");
    }

    [Theory]
    [InlineData("valid_user")]
    [InlineData("User123")]
    [InlineData("test_user_99")]
    public void Username_WhenValidFormat_ShouldNotHaveError(string username)
    {
        var dto = ValidDto();
        dto.Username = username;
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Username);
    }

    #endregion

    #region Password Validation

    [Fact]
    public void Password_WhenEmpty_ShouldHaveError()
    {
        var dto = ValidDto();
        dto.Password = "";
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Password_WhenTooShort_ShouldHaveError()
    {
        var dto = ValidDto();
        dto.Password = "12345";
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password must be at least 6 characters");
    }

    [Fact]
    public void Password_WhenTooLong_ShouldHaveError()
    {
        var dto = ValidDto();
        dto.Password = new string('a', 101);
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Password_WhenValid_ShouldNotHaveError()
    {
        var dto = ValidDto();
        dto.Password = "securePass123";
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }

    #endregion

    #region Role Validation

    [Fact]
    public void Role_WhenInvalid_ShouldHaveError()
    {
        var dto = ValidDto();
        dto.Role = (UserRole)99;
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Role);
    }

    [Theory]
    [InlineData(UserRole.Customer)]
    [InlineData(UserRole.Seller)]
    [InlineData(UserRole.Admin)]
    public void Role_WhenValid_ShouldNotHaveError(UserRole role)
    {
        var dto = ValidDto();
        dto.Role = role;
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Role);
    }

    #endregion

    #region Full DTO Validation

    [Fact]
    public void ValidDto_ShouldPassAllValidation()
    {
        var dto = ValidDto();
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyDto_ShouldHaveMultipleErrors()
    {
        var dto = new RegisterDto
        {
            Name = "",
            Email = "",
            Username = "",
            Password = ""
        };
        var result = _validator.TestValidate(dto);
        result.Errors.Should().HaveCountGreaterThanOrEqualTo(4);
    }

    #endregion
}
