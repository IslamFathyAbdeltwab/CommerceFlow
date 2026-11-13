using FluentValidation;

namespace CommerceFlow.Application.Returns.Commands.CompleteReturn;

public sealed class CompleteReturnCommandValidator : AbstractValidator<CompleteReturnCommand>
{
    public CompleteReturnCommandValidator()
    {
        RuleFor(x => x.ReturnId).NotEmpty();
    }
}
