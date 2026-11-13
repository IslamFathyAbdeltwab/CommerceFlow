using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Reviews.Interfaces;
using CommerceFlow.Domain.Entities.Reviews;
using CommerceFlow.Domain.Enums;
using MediatR;

namespace CommerceFlow.Application.Reviews.Commands.RejectReview;

public sealed class RejectReviewCommandHandler(
    IProductReviewRepository reviews,
    IUnitOfWork unitOfWork) : IRequestHandler<RejectReviewCommand>
{
    public async Task Handle(RejectReviewCommand request, CancellationToken cancellationToken)
    {
        var review = await reviews.GetByIdAsync(request.ReviewId, cancellationToken)
            ?? throw new NotFoundException(nameof(ProductReview), request.ReviewId);

        // pre-check before the state moves (state conflicts -> 409): Pending
        // is a first decision, Approved is a takedown - Rejected is terminal
        // (an appeal is a new review, never an un-reject)
        if (review.Status == ReviewStatus.Rejected)
            throw new ConflictException(
                $"Rejected reviews cannot be moderated again (current: {review.Status}).");

        review.Reject(request.Reason.Trim());

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
