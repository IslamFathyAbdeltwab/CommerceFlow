using MediatR;

namespace CommerceFlow.Application.Catalog.Products.Commands.RemoveVariantAttributeValue;

public sealed record RemoveVariantAttributeValueCommand(
    Guid ProductId,
    Guid VariantId,
    Guid AttributeId) : IRequest;
