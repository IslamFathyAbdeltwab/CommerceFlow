using CommerceFlow.Domain.Entities.Shipping;
using FluentValidation;

namespace CommerceFlow.Application.Shipping.Commands.FailShipment;

public sealed class FailShipmentCommandValidator : AbstractValidator<FailShipmentCommand>
{
    public FailShipmentCommandValidator()
    {
        RuleFor(x => x.ShipmentId).NotEmpty();

        RuleFor(x => x.Reason)
            .NotEmpty()
            .MaximumLength(Shipment.FailureReasonMaxLength);
    }
}
