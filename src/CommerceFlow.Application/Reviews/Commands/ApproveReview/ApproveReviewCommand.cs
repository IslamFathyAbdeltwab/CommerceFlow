using MediatR;

namespace CommerceFlow.Application.Reviews.Commands.ApproveReview;

// Moderator accepts the review - it becomes visible on the product page.
public sealed record ApproveReviewCommand(Guid ReviewId) : IRequest;
