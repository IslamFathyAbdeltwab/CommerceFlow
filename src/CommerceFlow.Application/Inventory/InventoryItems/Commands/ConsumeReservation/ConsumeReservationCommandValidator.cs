using FluentValidation;

namespace CommerceFlow.Application.Inventory.InventoryItems.Commands.ConsumeReservation;

public sealed class ConsumeReservationCommandValidator : AbstractValidator<ConsumeReservationCommand>
{
    public ConsumeReservationCommandValidator()
    {
        RuleFor(x => x.InventoryItemId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}
