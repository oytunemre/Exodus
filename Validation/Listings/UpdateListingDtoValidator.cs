using Exodus.Models.Dto.ListingDto;
using FluentValidation;

namespace Exodus.Validation.Listings;

public class UpdateListingDtoValidator : AbstractValidator<UpdateListingDto>
{
    public UpdateListingDtoValidator()
    {
        RuleFor(x => x.Price).GreaterThan(0);
        RuleFor(x => x.Stock).GreaterThanOrEqualTo(0);

        RuleFor(x => x.Condition)
            .IsInEnum()
            .When(x => x.Condition.HasValue);
    }
}
