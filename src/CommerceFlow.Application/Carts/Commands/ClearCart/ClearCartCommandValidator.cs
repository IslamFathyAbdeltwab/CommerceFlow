using FluentValidation;

namespace CommerceFlow.Application.Carts.Commands.ClearCart;

public sealed class ClearCartCommandValidator : AbstractValidator<ClearCartCommand>
{
    public ClearCartCommandValidator()
    {
        RuleFor(x => x.CartId).NotEmpty();
    }
}
