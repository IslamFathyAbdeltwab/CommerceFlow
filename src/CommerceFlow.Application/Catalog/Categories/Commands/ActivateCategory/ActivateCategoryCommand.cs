using MediatR;

namespace CommerceFlow.Application.Catalog.Categories.Commands.ActivateCategory;

public sealed record ActivateCategoryCommand(Guid CategoryId) : IRequest;
