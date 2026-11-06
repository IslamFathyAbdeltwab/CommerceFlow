using CommerceFlow.Application.Catalog.Interfaces;
using MediatR;

namespace CommerceFlow.Application.Catalog.Categories.Queries.GetCategoryById;

public sealed class GetCategoryByIdQueryHandler(
    ICategoryRepository categories) : IRequestHandler<GetCategoryByIdQuery, CategoryDetailDto?>
{
    public async Task<CategoryDetailDto?> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        var category = await categories.GetByIdAsync(request.CategoryId, cancellationToken);
        if (category is null)
            return null;

        return new CategoryDetailDto(
            category.Id,
            category.Name,
            category.Slug,
            category.Description,
            category.ParentCategoryId,
            category.IsActive,
            category.DisplayOrder,
            category.CreatedAtUtc,
            category.UpdatedAtUtc);
    }
}
