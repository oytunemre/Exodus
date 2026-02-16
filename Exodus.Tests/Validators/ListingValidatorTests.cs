using Exodus.Models.Dto.ListingDto;
using Exodus.Models.Enums;
using Exodus.Validation.Listings;
using FluentValidation.TestHelper;
using Xunit;

namespace Exodus.Tests.Validators;

public class AddListingDtoValidatorTests
{
    private readonly AddListingDtoValidator _validator = new();

    private static AddListingDto ValidDto() => new()
    {
        ProductId = 1,
        SellerId = 1,
        Price = 99.99m,
        Stock = 10,
        Condition = ListingCondition.New
    };

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ProductId_WhenNotPositive_ShouldHaveError(int productId)
    {
        var dto = ValidDto();
        dto.ProductId = productId;
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.ProductId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void SellerId_WhenNotPositive_ShouldHaveError(int sellerId)
    {
        var dto = ValidDto();
        dto.SellerId = sellerId;
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.SellerId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Price_WhenNotPositive_ShouldHaveError(decimal price)
    {
        var dto = ValidDto();
        dto.Price = price;
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Price);
    }

    [Fact]
    public void Stock_WhenNegative_ShouldHaveError()
    {
        var dto = ValidDto();
        dto.Stock = -1;
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Stock);
    }

    [Fact]
    public void Stock_WhenZero_ShouldNotHaveError()
    {
        var dto = ValidDto();
        dto.Stock = 0;
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Stock);
    }

    [Fact]
    public void Condition_WhenInvalid_ShouldHaveError()
    {
        var dto = ValidDto();
        dto.Condition = (ListingCondition)99;
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Condition);
    }

    [Fact]
    public void ValidDto_ShouldPassAllValidation()
    {
        var dto = ValidDto();
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveAnyValidationErrors();
    }
}

public class UpdateListingDtoValidatorTests
{
    private readonly UpdateListingDtoValidator _validator = new();

    private static UpdateListingDto ValidDto() => new()
    {
        Price = 99.99m,
        Stock = 10,
        Condition = ListingCondition.New,
        IsActive = true
    };

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Price_WhenNotPositive_ShouldHaveError(decimal price)
    {
        var dto = ValidDto();
        dto.Price = price;
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Price);
    }

    [Fact]
    public void Stock_WhenNegative_ShouldHaveError()
    {
        var dto = ValidDto();
        dto.Stock = -1;
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Stock);
    }

    [Fact]
    public void Condition_WhenNullable_ShouldNotHaveError()
    {
        var dto = ValidDto();
        dto.Condition = null;
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Condition);
    }

    [Fact]
    public void Condition_WhenInvalidEnumValue_ShouldHaveError()
    {
        var dto = ValidDto();
        dto.Condition = (ListingCondition)99;
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Condition);
    }

    [Fact]
    public void ValidDto_ShouldPassAllValidation()
    {
        var dto = ValidDto();
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
