using MediatR;

namespace CommerceFlow.Application.Catalog.Products.Commands.DeactivateVariant;

public sealed record DeactivateVariantCommand(Guid ProductId, Guid VariantId) : IRequest;
