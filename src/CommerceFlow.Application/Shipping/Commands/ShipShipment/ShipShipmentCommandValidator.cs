using FluentValidation;

namespace CommerceFlow.Application.Shipping.Commands.ShipShipment;

public sealed class ShipShipmentCommandValidator : AbstractValidator<ShipShipmentCommand>
{
    public ShipShipmentCommandValidator()
    {
        RuleFor(x => x.ShipmentId).NotEmpty();
    }
}
