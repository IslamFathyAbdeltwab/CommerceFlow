using MediatR;

namespace CommerceFlow.Application.Catalog.Categories.Queries.GetCategoryTree;

public sealed record GetCategoryTreeQuery(bool OnlyActive = true) : IRequest<IReadOnlyList<CategoryTreeDto>>;
