using CommerceFlow.Domain.Entities.Carts;
using FluentValidation;

namespace CommerceFlow.Application.Carts.Commands.AddCartItem;

public sealed class AddCartItemCommandValidator : AbstractValidator<AddCartItemCommand>
{
    public AddCartItemCommandValidator()
    {
        RuleFor(x => x.CartId).NotEmpty();
        RuleFor(x => x.ProductVariantId).NotEmpty();
        RuleFor(x => x.Quantity).InclusiveBetween(1, CartLine.MaxQuantity);
    }
}
