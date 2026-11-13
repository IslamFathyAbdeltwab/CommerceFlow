using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Reviews.Interfaces;
using CommerceFlow.Domain.Entities.Reviews;
using CommerceFlow.Domain.Enums;
using MediatR;

namespace CommerceFlow.Application.Reviews.Commands.ApproveReview;

public sealed class ApproveReviewCommandHandler(
    IProductReviewRepository reviews,
    IUnitOfWork unitOfWork) : IRequestHandler<ApproveReviewCommand>
{
    public async Task Handle(ApproveReviewCommand request, CancellationToken cancellationToken)
    {
        var review = await reviews.GetByIdAsync(request.ReviewId, cancellationToken)
            ?? throw new NotFoundException(nameof(ProductReview), request.ReviewId);

        // pre-check before the state moves (state conflicts -> 409): only a
        // pending review can be decided - a takedown is Reject, not un-approve
        if (review.Status != ReviewStatus.Pending)
            throw new ConflictException(
                $"Only pending reviews can be approved (current: {review.Status}).");

        review.Approve();

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
