using Exodus.Models.Dto.Payment;
using Exodus.Models.Enums;
using Exodus.Validation.Payments;
using FluentValidation.TestHelper;
using Xunit;

namespace Exodus.Tests.Validators;

public class CreatePaymentIntentDtoValidatorTests
{
    private readonly CreatePaymentIntentDtoValidator _validator = new();

    private static CreatePaymentIntentDto ValidDto() => new()
    {
        OrderId = 1,
        Currency = "TRY",
        Method = PaymentMethod.CreditCard
    };

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void OrderId_WhenNotPositive_ShouldHaveError(int orderId)
    {
        var dto = ValidDto();
        dto.OrderId = orderId;
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.OrderId);
    }

    [Fact]
    public void OrderId_WhenPositive_ShouldNotHaveError()
    {
        var dto = ValidDto();
        dto.OrderId = 5;
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.OrderId);
    }

    [Fact]
    public void Currency_WhenEmpty_ShouldHaveError()
    {
        var dto = ValidDto();
        dto.Currency = "";
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Currency);
    }

    [Theory]
    [InlineData("AB")]
    [InlineData("ABCD")]
    public void Currency_WhenNotThreeChars_ShouldHaveError(string currency)
    {
        var dto = ValidDto();
        dto.Currency = currency;
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Currency);
    }

    [Theory]
    [InlineData("TRY")]
    [InlineData("USD")]
    [InlineData("EUR")]
    public void Currency_WhenThreeChars_ShouldNotHaveError(string currency)
    {
        var dto = ValidDto();
        dto.Currency = currency;
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Currency);
    }

    [Fact]
    public void Method_WhenInvalidEnum_ShouldHaveError()
    {
        var dto = ValidDto();
        dto.Method = (PaymentMethod)99;
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Method);
    }

    [Theory]
    [InlineData(PaymentMethod.CreditCard)]
    [InlineData(PaymentMethod.DebitCard)]
    [InlineData(PaymentMethod.BankTransfer)]
    [InlineData(PaymentMethod.CashOnDelivery)]
    [InlineData(PaymentMethod.Wallet)]
    public void Method_WhenValidEnum_ShouldNotHaveError(PaymentMethod method)
    {
        var dto = ValidDto();
        dto.Method = method;
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Method);
    }

    [Fact]
    public void ValidDto_ShouldPassAllValidation()
    {
        var dto = ValidDto();
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
