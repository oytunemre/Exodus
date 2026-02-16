using Exodus.Models.Dto.CartDto;
using Exodus.Validation.Carts;
using FluentValidation.TestHelper;
using Xunit;

namespace Exodus.Tests.Validators;

public class AddToCartDtoValidatorTests
{
    private readonly AddToCartDtoValidator _validator = new();

    private static AddToCartDto ValidDto() => new()
    {
        UserId = 1,
        ListingId = 1,
        Quantity = 1
    };

    #region UserId

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void UserId_WhenNotPositive_ShouldHaveError(int userId)
    {
        var dto = ValidDto();
        dto.UserId = userId;
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(999)]
    public void UserId_WhenPositive_ShouldNotHaveError(int userId)
    {
        var dto = ValidDto();
        dto.UserId = userId;
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.UserId);
    }

    #endregion

    #region ListingId

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ListingId_WhenNotPositive_ShouldHaveError(int listingId)
    {
        var dto = ValidDto();
        dto.ListingId = listingId;
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.ListingId);
    }

    [Fact]
    public void ListingId_WhenPositive_ShouldNotHaveError()
    {
        var dto = ValidDto();
        dto.ListingId = 5;
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.ListingId);
    }

    #endregion

    #region Quantity

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-50)]
    public void Quantity_WhenNotPositive_ShouldHaveError(int quantity)
    {
        var dto = ValidDto();
        dto.Quantity = quantity;
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Quantity);
    }

    [Fact]
    public void Quantity_WhenExceedsMax_ShouldHaveError()
    {
        var dto = ValidDto();
        dto.Quantity = 1000;
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Quantity);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(999)]
    public void Quantity_WhenValid_ShouldNotHaveError(int quantity)
    {
        var dto = ValidDto();
        dto.Quantity = quantity;
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Quantity);
    }

    #endregion

    [Fact]
    public void ValidDto_ShouldPassAllValidation()
    {
        var dto = ValidDto();
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
