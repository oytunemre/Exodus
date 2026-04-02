using Exodus.Controllers.Seller;
using FluentValidation;

namespace Exodus.Validation.Listings;

public class SellerUpdateStockDtoValidator : AbstractValidator<SellerUpdateStockDto>
{
    public SellerUpdateStockDtoValidator()
    {
        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("StockQuantity cannot be negative.");
    }
}
