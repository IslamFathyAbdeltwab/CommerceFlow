using FluentValidation;

namespace CommerceFlow.Application.Inventory.InventoryItems.Commands.ShipStock;

public sealed class ShipStockCommandValidator : AbstractValidator<ShipStockCommand>
{
    public ShipStockCommandValidator()
    {
        RuleFor(x => x.InventoryItemId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}
