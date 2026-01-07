using FarmazonDemo.Models.Dto.Payment;
using FluentValidation;

namespace FarmazonDemo.Validation.Payments;

public class CreatePaymentIntentDtoValidator : AbstractValidator<CreatePaymentIntentDto>
{
    public CreatePaymentIntentDtoValidator()
    {
        RuleFor(x => x.OrderId).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.Method).IsInEnum();
    }
}
