using FluentValidation;

namespace CommerceFlow.Application.Shipping.Commands.MarkShipmentInTransit;

public sealed class MarkShipmentInTransitCommandValidator : AbstractValidator<MarkShipmentInTransitCommand>
{
    public MarkShipmentInTransitCommandValidator()
    {
        RuleFor(x => x.ShipmentId).NotEmpty();
    }
}
