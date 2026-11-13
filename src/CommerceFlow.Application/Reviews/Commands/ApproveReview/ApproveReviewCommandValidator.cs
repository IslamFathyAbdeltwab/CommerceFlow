using FluentValidation;

namespace CommerceFlow.Application.Reviews.Commands.ApproveReview;

public sealed class ApproveReviewCommandValidator : AbstractValidator<ApproveReviewCommand>
{
    public ApproveReviewCommandValidator()
    {
        RuleFor(x => x.ReviewId).NotEmpty();
    }
}
