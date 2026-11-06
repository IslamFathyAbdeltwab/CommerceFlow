using CommerceFlow.Domain.Enums;
using MediatR;

namespace CommerceFlow.Application.Catalog.ProductAttributes.Commands.UpdateProductAttribute;

public sealed record UpdateProductAttributeCommand(
    Guid AttributeId,
    string Name,
    string Code,
    ProductAttributeDataType DataType,
    bool IsVariantDefining,
    bool IsFilterable) : IRequest;
