using FluentValidation;

namespace CommerceFlow.Application.Shipping.Commands.CancelShipment;

public sealed class CancelShipmentCommandValidator : AbstractValidator<CancelShipmentCommand>
{
    public CancelShipmentCommandValidator()
    {
        RuleFor(x => x.ShipmentId).NotEmpty();
    }
}
