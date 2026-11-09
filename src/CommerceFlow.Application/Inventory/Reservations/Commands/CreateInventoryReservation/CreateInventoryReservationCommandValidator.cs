using FluentValidation;

namespace CommerceFlow.Application.Inventory.Reservations.Commands.CreateInventoryReservation;

public sealed class CreateInventoryReservationCommandValidator : AbstractValidator<CreateInventoryReservationCommand>
{
    public CreateInventoryReservationCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.Items).NotEmpty();

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.WarehouseId).NotEmpty();
            item.RuleFor(i => i.ProductVariantId).NotEmpty();
            item.RuleFor(i => i.Quantity).GreaterThan(0);
        });

        RuleFor(x => x.ExpiresAtUtc)
            .GreaterThan(DateTimeOffset.UtcNow)
            .WithMessage("Expires-at must be in the future.")
            .When(x => x.ExpiresAtUtc.HasValue);
    }
}
