using MediatR;

namespace CommerceFlow.Application.Catalog.Products.Commands.SetVariantAttributeValue;

public sealed record SetVariantAttributeValueCommand(
    Guid ProductId,
    Guid VariantId,
    Guid AttributeId,
    string Value) : IRequest;
