using CommerceFlow.Application.Catalog.Interfaces;
using CommerceFlow.Application.Catalog.Products.Queries;
using MediatR;

namespace CommerceFlow.Application.Catalog.ProductAttributes.Queries.GetProductAttributes;

public sealed class GetProductAttributesQueryHandler(
    IProductAttributeRepository attributes) : IRequestHandler<GetProductAttributesQuery, PagedResult<ProductAttributeListItemDto>>
{
    public async Task<PagedResult<ProductAttributeListItemDto>> Handle(GetProductAttributesQuery request, CancellationToken cancellationToken)
    {
        var totalCount = await attributes.CountAsync(
            request.SearchTerm?.Trim(),
            request.IsVariantDefining,
            request.IsFilterable,
            cancellationToken);

        var list = await attributes.GetAllAsync(
            request.SearchTerm?.Trim(),
            request.IsVariantDefining,
            request.IsFilterable,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var items = list.Select(a => new ProductAttributeListItemDto(
            a.Id,
            a.Name,
            a.Code,
            a.DataType,
            a.IsVariantDefining,
            a.IsFilterable)).ToList();

        return new PagedResult<ProductAttributeListItemDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}
