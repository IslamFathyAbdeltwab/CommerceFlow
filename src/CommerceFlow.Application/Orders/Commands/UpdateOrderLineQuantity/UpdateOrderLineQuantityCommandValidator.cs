using FluentValidation;

namespace CommerceFlow.Application.Orders.Commands.UpdateOrderLineQuantity;

public sealed class UpdateOrderLineQuantityCommandValidator : AbstractValidator<UpdateOrderLineQuantityCommand>
{
    public UpdateOrderLineQuantityCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.OrderLineId).NotEmpty();
        RuleFor(x => x.Quantity).InclusiveBetween(1, 999);
    }
}
