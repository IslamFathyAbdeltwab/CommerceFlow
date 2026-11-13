using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Reviews.Interfaces;
using CommerceFlow.Domain.Entities.Reviews;
using CommerceFlow.Domain.Enums;
using MediatR;

namespace CommerceFlow.Application.Reviews.Commands.ReplyToReview;

public sealed class ReplyToReviewCommandHandler(
    IProductReviewRepository reviews,
    IUnitOfWork unitOfWork) : IRequestHandler<ReplyToReviewCommand>
{
    public async Task Handle(ReplyToReviewCommand request, CancellationToken cancellationToken)
    {
        var review = await reviews.GetByIdAsync(request.ReviewId, cancellationToken)
            ?? throw new NotFoundException(nameof(ProductReview), request.ReviewId);

        // pre-check before anything is written (state conflicts -> 409):
        // only content that is actually published can be answered
        if (review.Status != ReviewStatus.Approved)
            throw new ConflictException(
                $"Only approved reviews can receive a reply (current: {review.Status}).");

        review.Reply(request.Reply.Trim());

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
