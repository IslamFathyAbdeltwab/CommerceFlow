using FluentValidation;

namespace CommerceFlow.Application.Orders.Commands.PlaceOrder;

public sealed class PlaceOrderCommandValidator : AbstractValidator<PlaceOrderCommand>
{
    public PlaceOrderCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.CustomerNote).MaximumLength(1000).When(x => x.CustomerNote is not null);
        RuleFor(x => x.ShippingFee).GreaterThanOrEqualTo(0);
        RuleFor(x => x.DiscountAmount).GreaterThanOrEqualTo(0);

        RuleFor(x => x.Lines).NotEmpty();
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductVariantId).NotEmpty();
            line.RuleFor(l => l.Sku).NotEmpty().MaximumLength(100);
            line.RuleFor(l => l.ProductName).NotEmpty().MaximumLength(300);
            line.RuleFor(l => l.UnitPrice).GreaterThanOrEqualTo(0);
            line.RuleFor(l => l.Quantity).InclusiveBetween(1, 999);
        });
        RuleFor(x => x.Lines)
            .Must(lines => lines.Select(l => l.ProductVariantId).Distinct().Count() == lines.Count)
            .WithMessage("Order must not contain duplicate variants; use one line per variant.");

        RuleFor(x => x.ShippingAddress).NotNull();
        When(x => x.ShippingAddress is not null, () =>
        {
            RuleFor(x => x.ShippingAddress.FullName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.ShippingAddress.Line1).NotEmpty().MaximumLength(200);
            RuleFor(x => x.ShippingAddress.Line2).MaximumLength(200).When(x => x.ShippingAddress.Line2 is not null);
            RuleFor(x => x.ShippingAddress.City).NotEmpty().MaximumLength(100);
            RuleFor(x => x.ShippingAddress.State).MaximumLength(100).When(x => x.ShippingAddress.State is not null);
            RuleFor(x => x.ShippingAddress.PostalCode).NotEmpty().MaximumLength(20);
            RuleFor(x => x.ShippingAddress.Country).NotEmpty().MaximumLength(100);
            RuleFor(x => x.ShippingAddress.PhoneNumber).MaximumLength(30).When(x => x.ShippingAddress.PhoneNumber is not null);
        });
    }
}
