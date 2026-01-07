using FarmazonDemo.Models.Dto.Shipment;
using FluentValidation;

namespace FarmazonDemo.Validation.Shipments;

public class ShipSellerOrderDtoValidator : AbstractValidator<ShipSellerOrderDto>
{
    public ShipSellerOrderDtoValidator()
    {
        RuleFor(x => x.Carrier).NotEmpty().MaximumLength(50);
        RuleFor(x => x.TrackingNumber).NotEmpty().MaximumLength(80);
    }
}
