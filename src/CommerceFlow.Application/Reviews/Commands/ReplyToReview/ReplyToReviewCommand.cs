using MediatR;

namespace CommerceFlow.Application.Reviews.Commands.ReplyToReview;

// Merchant answers a published review; re-replying updates the answer.
public sealed record ReplyToReviewCommand(Guid ReviewId, string Reply) : IRequest;
