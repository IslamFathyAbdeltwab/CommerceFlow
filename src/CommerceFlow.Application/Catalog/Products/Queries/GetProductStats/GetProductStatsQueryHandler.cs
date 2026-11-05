//using CommerceFlow.Abstraction.Catalog.Interfaces;
using CommerceFlow.Application.Catalog.Interfaces;
using CommerceFlow.Domain.Enums;
using MediatR;

namespace CommerceFlow.Application.Catalog.Products.Queries.GetProductStats;

public sealed class GetProductStatsQueryHandler(
    IProductRepository products) : IRequestHandler<GetProductStatsQuery, ProductStatsDto>
{
    public async Task<ProductStatsDto> Handle(GetProductStatsQuery request, CancellationToken cancellationToken)
    {
        var draft = await products.CountByStatusAsync(ProductStatus.Draft, cancellationToken);
        var published = await products.CountByStatusAsync(ProductStatus.Published, cancellationToken);
        var archived = await products.CountByStatusAsync(ProductStatus.Archived, cancellationToken);

        return new ProductStatsDto(draft, published, archived, draft + published + archived);
    }
}