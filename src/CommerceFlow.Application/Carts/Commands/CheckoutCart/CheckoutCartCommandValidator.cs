using FluentValidation;

namespace CommerceFlow.Application.Carts.Commands.CheckoutCart;

public sealed class CheckoutCartCommandValidator : AbstractValidator<CheckoutCartCommand>
{
    public CheckoutCartCommandValidator()
    {
        RuleFor(x => x.CartId).NotEmpty();
    }
}
