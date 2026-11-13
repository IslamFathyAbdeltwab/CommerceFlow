using CommerceFlow.Domain.Entities.Reviews;
using FluentValidation;

namespace CommerceFlow.Application.Reviews.Commands.RejectReview;

public sealed class RejectReviewCommandValidator : AbstractValidator<RejectReviewCommand>
{
    public RejectReviewCommandValidator()
    {
        RuleFor(x => x.ReviewId).NotEmpty();

        RuleFor(x => x.Reason)
            .NotEmpty()
            .MaximumLength(ProductReview.ModerationReasonMaxLength);
    }
}
