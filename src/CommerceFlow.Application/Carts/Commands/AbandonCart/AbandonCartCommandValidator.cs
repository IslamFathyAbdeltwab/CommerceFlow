using FluentValidation;

namespace CommerceFlow.Application.Carts.Commands.AbandonCart;

public sealed class AbandonCartCommandValidator : AbstractValidator<AbandonCartCommand>
{
    public AbandonCartCommandValidator()
    {
        RuleFor(x => x.CartId).NotEmpty();
    }
}
