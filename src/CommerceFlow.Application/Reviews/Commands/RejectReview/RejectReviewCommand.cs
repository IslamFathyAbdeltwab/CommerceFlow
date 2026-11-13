using MediatR;

namespace CommerceFlow.Application.Reviews.Commands.RejectReview;

// Moderator declines a new review or takes down published content (spam,
// abuse, policy). The reason is kept for the moderation audit trail.
public sealed record RejectReviewCommand(Guid ReviewId, string Reason) : IRequest;
