using CommerceFlow.Domain.Entities.Returns;
using FluentValidation;

namespace CommerceFlow.Application.Returns.Commands.RejectReturn;

public sealed class RejectReturnCommandValidator : AbstractValidator<RejectReturnCommand>
{
    public RejectReturnCommandValidator()
    {
        RuleFor(x => x.ReturnId).NotEmpty();

        RuleFor(x => x.Reason)
            .NotEmpty()
            .MaximumLength(ReturnRequest.RejectionReasonMaxLength);
    }
}
