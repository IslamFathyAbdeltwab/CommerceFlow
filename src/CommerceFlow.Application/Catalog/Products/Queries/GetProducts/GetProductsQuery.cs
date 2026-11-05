using MediatR;

namespace CommerceFlow.Application.Catalog.Products.Queries.GetProducts;

public sealed record GetProductsQuery(
    string? SearchTerm = null,
    Guid? BrandId = null,
    Guid? CategoryId = null,
    bool? IsFeatured = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PagedResult<ProductListItemDto>>;