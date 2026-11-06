using MediatR;

namespace CommerceFlow.Application.Catalog.Categories.Queries.GetCategoryById;

public sealed record GetCategoryByIdQuery(Guid CategoryId) : IRequest<CategoryDetailDto?>;
