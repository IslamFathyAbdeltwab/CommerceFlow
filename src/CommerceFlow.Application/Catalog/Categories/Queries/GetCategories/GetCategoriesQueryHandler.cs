using CommerceFlow.Application.Catalog.Interfaces;
using CommerceFlow.Application.Catalog.Products.Queries;
using MediatR;

namespace CommerceFlow.Application.Catalog.Categories.Queries.GetCategories;

public sealed class GetCategoriesQueryHandler(
    ICategoryRepository categories) : IRequestHandler<GetCategoriesQuery, PagedResult<CategoryListItemDto>>
{
    public async Task<PagedResult<CategoryListItemDto>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        var totalCount = await categories.CountAsync(
            request.SearchTerm?.Trim(),
            request.IsActive,
            cancellationToken);

        var list = await categories.GetAllAsync(
            request.SearchTerm?.Trim(),
            request.IsActive,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var items = list.Select(c => new CategoryListItemDto(
            c.Id,
            c.Name,
            c.Slug,
            c.Description,
            c.ParentCategoryId,
            c.IsActive,
            c.DisplayOrder,
            c.CreatedAtUtc,
            c.UpdatedAtUtc)).ToList();

        return new PagedResult<CategoryListItemDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}
