using FluentValidation;

namespace CommerceFlow.Application.Orders.Commands.AddOrderLine;

public sealed class AddOrderLineCommandValidator : AbstractValidator<AddOrderLineCommand>
{
    public AddOrderLineCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.ProductVariantId).NotEmpty();
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ProductName).NotEmpty().MaximumLength(300);
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Quantity).InclusiveBetween(1, 999);
    }
}
