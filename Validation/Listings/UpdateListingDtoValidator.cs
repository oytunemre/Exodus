using FarmazonDemo.Models.Dto.ListingDto;
using FluentValidation;

public class UpdateListingDtoValidator : AbstractValidator<UpdateListingDto>
{
    public UpdateListingDtoValidator()
    {
        RuleFor(x => x.Price).GreaterThan(0);
        RuleFor(x => x.Stock).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Condition).MaximumLength(50);
        // IsActive bool zaten ok
    }
}