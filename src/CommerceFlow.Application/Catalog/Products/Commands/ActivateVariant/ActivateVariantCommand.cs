using MediatR;

namespace CommerceFlow.Application.Catalog.Products.Commands.ActivateVariant;

public sealed record ActivateVariantCommand(Guid ProductId, Guid VariantId) : IRequest;
