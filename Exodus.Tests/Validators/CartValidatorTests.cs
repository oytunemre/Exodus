using Exodus.Models.Dto.CartDto;
using FluentValidation.TestHelper;
using Xunit;

namespace Exodus.Tests.Validators;

public class UpdateCartItemDtoValidatorTests
{
    private readonly UpdateCartItemDtoValidator _validator = new();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Quantity_WhenNotPositive_ShouldHaveError(int quantity)
    {
        var dto = new UpdateCartItemDto { Quantity = quantity };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Quantity);
    }

    [Fact]
    public void Quantity_WhenExceedsMax_ShouldHaveError()
    {
        var dto = new UpdateCartItemDto { Quantity = 1000 };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Quantity);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(500)]
    [InlineData(999)]
    public void Quantity_WhenValid_ShouldNotHaveError(int quantity)
    {
        var dto = new UpdateCartItemDto { Quantity = quantity };
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Quantity);
    }
}
