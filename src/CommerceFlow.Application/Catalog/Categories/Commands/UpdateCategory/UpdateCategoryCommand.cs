using MediatR;

namespace CommerceFlow.Application.Catalog.Categories.Commands.UpdateCategory;

public sealed record UpdateCategoryCommand(
    Guid CategoryId,
    string Name,
    string Slug,
    string? Description,
    int DisplayOrder,
    Guid? ParentCategoryId) : IRequest;
