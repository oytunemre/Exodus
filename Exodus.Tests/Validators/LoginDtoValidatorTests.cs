using Exodus.Models.Dto;
using Exodus.Validation;
using FluentAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace Exodus.Tests.Validators;

public class LoginDtoValidatorTests
{
    private readonly LoginDtoValidator _validator = new();

    private static LoginDto ValidDto() => new()
    {
        EmailOrUsername = "testuser",
        Password = "password123"
    };

    [Fact]
    public void EmailOrUsername_WhenEmpty_ShouldHaveError()
    {
        var dto = ValidDto();
        dto.EmailOrUsername = "";
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.EmailOrUsername)
            .WithErrorMessage("Email or username is required");
    }

    [Fact]
    public void EmailOrUsername_WhenTooShort_ShouldHaveError()
    {
        var dto = ValidDto();
        dto.EmailOrUsername = "ab";
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.EmailOrUsername)
            .WithErrorMessage("Email or username must be at least 3 characters");
    }

    [Theory]
    [InlineData("testuser")]
    [InlineData("test@example.com")]
    [InlineData("user123")]
    public void EmailOrUsername_WhenValid_ShouldNotHaveError(string value)
    {
        var dto = ValidDto();
        dto.EmailOrUsername = value;
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.EmailOrUsername);
    }

    [Fact]
    public void Password_WhenEmpty_ShouldHaveError()
    {
        var dto = ValidDto();
        dto.Password = "";
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password is required");
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
    public void Password_WhenValid_ShouldNotHaveError()
    {
        var dto = ValidDto();
        dto.Password = "password123";
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }

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
        var dto = new LoginDto
        {
            EmailOrUsername = "",
            Password = ""
        };
        var result = _validator.TestValidate(dto);
        result.Errors.Should().HaveCountGreaterThanOrEqualTo(2);
    }
}
