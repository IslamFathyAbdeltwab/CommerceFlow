using CommerceFlow.Domain.Entities.Reviews;
using FluentValidation;

namespace CommerceFlow.Application.Reviews.Commands.ReplyToReview;

public sealed class ReplyToReviewCommandValidator : AbstractValidator<ReplyToReviewCommand>
{
    public ReplyToReviewCommandValidator()
    {
        RuleFor(x => x.ReviewId).NotEmpty();

        RuleFor(x => x.Reply)
            .NotEmpty()
            .MaximumLength(ProductReview.SellerReplyMaxLength);
    }
}
