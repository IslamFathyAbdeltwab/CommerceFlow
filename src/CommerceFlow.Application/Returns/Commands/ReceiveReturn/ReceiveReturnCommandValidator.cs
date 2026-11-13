using FluentValidation;

namespace CommerceFlow.Application.Returns.Commands.ReceiveReturn;

public sealed class ReceiveReturnCommandValidator : AbstractValidator<ReceiveReturnCommand>
{
    public ReceiveReturnCommandValidator()
    {
        RuleFor(x => x.ReturnId).NotEmpty();
    }
}
