using FluentValidation;

namespace CommerceFlow.Application.Carts.Commands.CreateCart;

public sealed class CreateCartCommandValidator : AbstractValidator<CreateCartCommand>
{
    public CreateCartCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
    }
}
