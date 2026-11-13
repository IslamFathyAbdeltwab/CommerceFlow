using MediatR;

namespace CommerceFlow.Application.Reviews.Commands.CreateReview;

// A customer's rating of a product. OrderId is optional: when present the
// handler proves the order belongs to the reviewer and the DTO carries the
// verified-purchase badge. One review per customer per product - the handler
// pre-checks and answers 409. Returns the review id.
public sealed record CreateReviewCommand(
    Guid ProductId,
    Guid CustomerId,
    int Rating,
    string Comment,
    string? Title = null,
    Guid? OrderId = null) : IRequest<Guid>;
