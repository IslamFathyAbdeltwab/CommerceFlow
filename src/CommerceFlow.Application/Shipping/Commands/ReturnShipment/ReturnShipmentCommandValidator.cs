using FluentValidation;

namespace CommerceFlow.Application.Shipping.Commands.ReturnShipment;

public sealed class ReturnShipmentCommandValidator : AbstractValidator<ReturnShipmentCommand>
{
    public ReturnShipmentCommandValidator()
    {
        RuleFor(x => x.ShipmentId).NotEmpty();
    }
}
