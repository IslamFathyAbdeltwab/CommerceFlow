using MediatR;

namespace CommerceFlow.Application.Catalog.Products.Commands.RemoveVariant;

public sealed record RemoveVariantCommand(Guid ProductId, Guid VariantId) : IRequest;
