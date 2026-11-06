using CommerceFlow.Application.Catalog.Products.Queries;
using MediatR;

namespace CommerceFlow.Application.Catalog.ProductAttributes.Queries.GetProductAttributes;

public sealed record GetProductAttributesQuery(
    string? SearchTerm = null,
    bool? IsVariantDefining = null,
    bool? IsFilterable = null,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PagedResult<ProductAttributeListItemDto>>;
