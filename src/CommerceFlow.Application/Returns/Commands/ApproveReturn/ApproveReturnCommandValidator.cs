using FluentValidation;

namespace CommerceFlow.Application.Returns.Commands.ApproveReturn;

public sealed class ApproveReturnCommandValidator : AbstractValidator<ApproveReturnCommand>
{
    public ApproveReturnCommandValidator()
    {
        RuleFor(x => x.ReturnId).NotEmpty();
    }
}
