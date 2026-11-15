using CommerceFlow.Application.Promotions.Interfaces;
using MediatR;

namespace CommerceFlow.Application.Promotions.Queries.GetPromotionById;

public sealed class GetPromotionByIdQueryHandler(IPromotionRepository promotions)
    : IRequestHandler<GetPromotionByIdQuery, PromotionDto?>
{
    public async Task<PromotionDto?> Handle(GetPromotionByIdQuery request, CancellationToken cancellationToken)
    {
        var promotion = await promotions.GetByIdAsync(request.PromotionId, cancellationToken);

        return promotion is null ? null : PromotionMappings.ToDto(promotion);
    }
}
