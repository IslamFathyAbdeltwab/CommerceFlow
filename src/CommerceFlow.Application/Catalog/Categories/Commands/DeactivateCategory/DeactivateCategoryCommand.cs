using MediatR;

namespace CommerceFlow.Application.Catalog.Categories.Commands.DeactivateCategory;

public sealed record DeactivateCategoryCommand(Guid CategoryId) : IRequest;
