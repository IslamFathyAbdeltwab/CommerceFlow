using CommerceFlow.Application.Catalog.Interfaces;
using CommerceFlow.Domain.Entities.Catalog;
using MediatR;

namespace CommerceFlow.Application.Catalog.Categories.Queries.GetCategoryTree;

public sealed class GetCategoryTreeQueryHandler(
    ICategoryRepository categories) : IRequestHandler<GetCategoryTreeQuery, IReadOnlyList<CategoryTreeDto>>
{
    public async Task<IReadOnlyList<CategoryTreeDto>> Handle(GetCategoryTreeQuery request, CancellationToken cancellationToken)
    {
        var all = await categories.GetAllAsync(null, request.OnlyActive ? true : null, 1, int.MaxValue, cancellationToken);

        var ids = all.Select(c => c.Id).ToHashSet();
        var childrenByParent = all
            .GroupBy(c => c.ParentCategoryId ?? Guid.Empty)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name).ToList());

        CategoryTreeDto Build(Category c)
        {
            var children = childrenByParent.TryGetValue(c.Id, out var kids)
                ? kids.Select(Build).ToList()
                : [];

            return new CategoryTreeDto(c.Id, c.Name, c.Slug, c.DisplayOrder, children);
        }

        var roots = all
            .Where(c => c.ParentCategoryId is null || !ids.Contains(c.ParentCategoryId.Value))
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .Select(Build)
            .ToList();

        return roots;
    }
}
