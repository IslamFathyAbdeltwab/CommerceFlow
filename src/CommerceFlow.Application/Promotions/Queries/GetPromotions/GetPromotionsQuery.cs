using CommerceFlow.Application.Catalog.Products.Queries;
using CommerceFlow.Domain.Enums;
using MediatR;

namespace CommerceFlow.Application.Promotions.Queries.GetPromotions;

public sealed record GetPromotionsQuery(
    string? Code = null,
    PromotionStatus? Status = null,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PagedResult<PromotionDto>>;
