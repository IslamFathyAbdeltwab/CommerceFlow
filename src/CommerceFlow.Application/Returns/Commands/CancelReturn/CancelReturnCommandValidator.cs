using FluentValidation;

namespace CommerceFlow.Application.Returns.Commands.CancelReturn;

public sealed class CancelReturnCommandValidator : AbstractValidator<CancelReturnCommand>
{
    public CancelReturnCommandValidator()
    {
        RuleFor(x => x.ReturnId).NotEmpty();
    }
}
