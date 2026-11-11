using CommerceFlow.Domain.Entities.Carts;
using FluentValidation;

namespace CommerceFlow.Application.Carts.Commands.ChangeCartItemQuantity;

public sealed class ChangeCartItemQuantityCommandValidator : AbstractValidator<ChangeCartItemQuantityCommand>
{
    public ChangeCartItemQuantityCommandValidator()
    {
        RuleFor(x => x.CartId).NotEmpty();
        RuleFor(x => x.CartLineId).NotEmpty();
        RuleFor(x => x.Quantity).InclusiveBetween(1, CartLine.MaxQuantity);
    }
}
