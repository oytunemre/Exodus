using Exodus.Controllers.Seller;
using FluentValidation;

namespace Exodus.Validation.Listings;

public class SellerUpdateListingDtoValidator : AbstractValidator<SellerUpdateListingDto>
{
    public SellerUpdateListingDtoValidator()
    {
        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than 0.")
            .When(x => x.Price.HasValue);

        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("StockQuantity cannot be negative.")
            .When(x => x.StockQuantity.HasValue);

        RuleFor(x => x.LowStockThreshold)
            .GreaterThanOrEqualTo(0).WithMessage("LowStockThreshold cannot be negative.")
            .When(x => x.LowStockThreshold.HasValue);

        RuleFor(x => x.SKU)
            .MaximumLength(50).WithMessage("SKU cannot exceed 50 characters.")
            .When(x => x.SKU != null);

        RuleFor(x => x.Condition)
            .IsInEnum().WithMessage("Invalid listing condition.")
            .When(x => x.Condition.HasValue);
    }
}
