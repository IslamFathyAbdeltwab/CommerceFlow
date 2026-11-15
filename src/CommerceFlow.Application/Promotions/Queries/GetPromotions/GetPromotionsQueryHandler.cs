using CommerceFlow.Application.Catalog.Products.Queries;
using CommerceFlow.Application.Promotions.Interfaces;
using MediatR;

namespace CommerceFlow.Application.Promotions.Queries.GetPromotions;

public sealed class GetPromotionsQueryHandler(IPromotionRepository promotions)
    : IRequestHandler<GetPromotionsQuery, PagedResult<PromotionDto>>
{
    public async Task<PagedResult<PromotionDto>> Handle(GetPromotionsQuery request, CancellationToken cancellationToken)
    {
        var totalCount = await promotions.CountAsync(
            request.Code,
            request.Status,
            cancellationToken);

        var list = await promotions.GetAllAsync(
            request.Code,
            request.Status,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var items = list.Select(PromotionMappings.ToDto).ToList();

        return new PagedResult<PromotionDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}
