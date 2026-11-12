using CommerceFlow.Domain.Entities.Shipping;
using FluentValidation;

namespace CommerceFlow.Application.Shipping.Commands.CreateShipment;

public sealed class CreateShipmentCommandValidator : AbstractValidator<CreateShipmentCommand>
{
    public CreateShipmentCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();

        RuleFor(x => x.Carrier)
            .NotEmpty()
            .MaximumLength(Shipment.CarrierMaxLength);
        RuleFor(x => x.TrackingNumber)
            .NotEmpty()
            .MaximumLength(Shipment.TrackingNumberMaxLength);
        RuleFor(x => x.ServiceLevel)
            .MaximumLength(Shipment.ServiceLevelMaxLength);

        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductVariantId).NotEmpty();
            item.RuleFor(i => i.Quantity).GreaterThan(0);
        });
    }
}
