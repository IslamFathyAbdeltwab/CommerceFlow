using FluentValidation;

namespace CommerceFlow.Application.Carts.Commands.CheckoutCart;

public sealed class CheckoutCartCommandValidator : AbstractValidator<CheckoutCartCommand>
{
    public CheckoutCartCommandValidator()
    {
        RuleFor(x => x.CartId).NotEmpty();

        RuleFor(x => x.CustomerNote).MaximumLength(1000).When(x => x.CustomerNote is not null);

        // checkout needs somewhere to ship - same shape rules as PlaceOrder
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
