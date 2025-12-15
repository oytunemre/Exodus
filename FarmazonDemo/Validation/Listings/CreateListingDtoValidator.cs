using FluentValidation;
using FarmazonDemo.Models.Dto.ListingDto;

namespace FarmazonDemo.Validation.Listings;

public class CreateListingDtoValidator : AbstractValidator<CreateListingDto>
{
    public CreateListingDtoValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.SellerId).GreaterThan(0);
        RuleFor(x => x.Price).GreaterThan(0);
        RuleFor(x => x.Stock).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Condition).MaximumLength(50);
    }
}

