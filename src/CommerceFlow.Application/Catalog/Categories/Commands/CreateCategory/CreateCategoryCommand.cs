using MediatR;

namespace CommerceFlow.Application.Catalog.Categories.Commands.CreateCategory;

public sealed record CreateCategoryCommand(
    string Name,
    string Slug,
    string? Description = null,
    int DisplayOrder = 0,
    Guid? ParentCategoryId = null) : IRequest<Guid>;
