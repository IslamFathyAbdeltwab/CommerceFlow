using CommerceFlow.Application.Catalog.Products.Queries;
using MediatR;

namespace CommerceFlow.Application.Catalog.Categories.Queries.GetCategories;

public sealed record GetCategoriesQuery(
    string? SearchTerm = null,
    bool? IsActive = null,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PagedResult<CategoryListItemDto>>;
