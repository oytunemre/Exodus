using Exodus.Models.Dto.ListingDto;
using FluentValidation;

namespace Exodus.Validation.Listings;

public class AddListingDtoValidator : AbstractValidator<AddListingDto>
{
    public AddListingDtoValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.SellerId).GreaterThan(0);

        RuleFor(x => x.Price).GreaterThan(0);
        RuleFor(x => x.Stock).GreaterThanOrEqualTo(0);

        RuleFor(x => x.Condition).IsInEnum();
    }
}
