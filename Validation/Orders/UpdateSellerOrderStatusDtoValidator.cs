using Exodus.Controllers.Seller;
using FluentValidation;

namespace Exodus.Validation.Orders;

public class UpdateSellerOrderStatusDtoValidator : AbstractValidator<UpdateSellerOrderStatusDto>
{
    public UpdateSellerOrderStatusDtoValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid order status value.");
    }
}
