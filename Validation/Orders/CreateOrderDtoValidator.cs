using Exodus.Models.Dto;
using FluentValidation;

namespace Exodus.Validation.Orders;

public class CreateOrderDtoValidator : AbstractValidator<CreateOrderDto>
{
    public CreateOrderDtoValidator()
    {
        RuleFor(x => x.ShippingAddressId)
            .GreaterThan(0).WithMessage("A valid shipping address must be selected.");

        RuleFor(x => x.CustomerNote)
            .MaximumLength(1000).WithMessage("Customer note cannot exceed 1000 characters.")
            .When(x => x.CustomerNote != null);

        RuleFor(x => x.CouponCode)
            .MaximumLength(50).WithMessage("Coupon code cannot exceed 50 characters.")
            .When(x => x.CouponCode != null);
    }
}
