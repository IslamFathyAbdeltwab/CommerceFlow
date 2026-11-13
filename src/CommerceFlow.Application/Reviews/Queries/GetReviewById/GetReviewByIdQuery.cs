using MediatR;

namespace CommerceFlow.Application.Reviews.Queries.GetReviewById;

// null = not found (the controller maps it to 404, same as GetShipmentById).
public sealed record GetReviewByIdQuery(Guid ReviewId) : IRequest<ProductReviewDto?>;
