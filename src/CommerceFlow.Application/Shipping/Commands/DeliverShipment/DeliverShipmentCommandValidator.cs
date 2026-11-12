using FluentValidation;

namespace CommerceFlow.Application.Shipping.Commands.DeliverShipment;

public sealed class DeliverShipmentCommandValidator : AbstractValidator<DeliverShipmentCommand>
{
    public DeliverShipmentCommandValidator()
    {
        RuleFor(x => x.ShipmentId).NotEmpty();
    }
}
