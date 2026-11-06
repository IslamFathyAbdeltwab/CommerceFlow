using CommerceFlow.Application.Catalog.Products.Queries;
using MediatR;

namespace CommerceFlow.Application.Catalog.Brands.Queries.GetBrands;

public sealed record GetBrandsQuery(
    string? SearchTerm = null,
    bool? IsActive = null,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PagedResult<BrandListItemDto>>;
