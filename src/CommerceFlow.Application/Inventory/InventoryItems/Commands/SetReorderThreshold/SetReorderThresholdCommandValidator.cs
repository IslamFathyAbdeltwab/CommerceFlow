using FluentValidation;

namespace CommerceFlow.Application.Inventory.InventoryItems.Commands.SetReorderThreshold;

public sealed class SetReorderThresholdCommandValidator : AbstractValidator<SetReorderThresholdCommand>
{
    public SetReorderThresholdCommandValidator()
    {
        RuleFor(x => x.InventoryItemId).NotEmpty();
        RuleFor(x => x.Threshold).GreaterThanOrEqualTo(0);
    }
}
