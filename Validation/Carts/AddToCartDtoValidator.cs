using FluentValidation;
using FarmazonDemo.Models.Dto.CartDto;

namespace FarmazonDemo.Validation.Carts;

public class AddToCartDtoValidator : AbstractValidator<AddToCartDto>
{
    public AddToCartDtoValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0);
        RuleFor(x => x.ListingId).GreaterThan(0);
        RuleFor(x => x.Quantity).GreaterThan(0).LessThanOrEqualTo(999);
    }
}