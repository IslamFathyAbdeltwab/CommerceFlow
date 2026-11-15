using MediatR;

namespace CommerceFlow.Application.Promotions.Queries.GetPromotionById;

// null = not found (the controller maps it to 404, same as GetReturnById).
public sealed record GetPromotionByIdQuery(Guid PromotionId) : IRequest<PromotionDto?>;
