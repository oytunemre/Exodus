using Exodus.Models.Dto.Payment;
using FluentValidation;

namespace Exodus.Validation.Payments;

public class CreatePaymentIntentDtoValidator : AbstractValidator<CreatePaymentIntentDto>
{
    public CreatePaymentIntentDtoValidator()
    {
        RuleFor(x => x.OrderId).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.Method).IsInEnum();

        // Validate nested CardDetails format only when CardDetails are provided
        When(x => x.CardDetails != null, () =>
        {
            RuleFor(x => x.CardDetails!.CardNumber)
                .NotEmpty().WithMessage("Card number is required.")
                .Matches(@"^\d{13,19}$").WithMessage("Card number must be 13–19 digits.");

            RuleFor(x => x.CardDetails!.ExpiryDate)
                .NotEmpty().WithMessage("Expiry date is required.")
                .Matches(@"^(0[1-9]|1[0-2])\/\d{2}$").WithMessage("Expiry date must be in MM/YY format.");

            RuleFor(x => x.CardDetails!.Cvv)
                .NotEmpty().WithMessage("CVV is required.")
                .Matches(@"^\d{3,4}$").WithMessage("CVV must be 3 or 4 digits.");

            RuleFor(x => x.CardDetails!.CardHolderName)
                .NotEmpty().WithMessage("Card holder name is required.")
                .MaximumLength(100).WithMessage("Card holder name cannot exceed 100 characters.");
        });

        RuleFor(x => x.InstallmentCount)
            .GreaterThan(1).WithMessage("Installment count must be greater than 1.")
            .When(x => x.InstallmentCount.HasValue);
    }
}
