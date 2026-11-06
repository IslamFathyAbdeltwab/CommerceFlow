using CommerceFlow.Domain.Enums;
using MediatR;

namespace CommerceFlow.Application.Catalog.ProductAttributes.Commands.CreateProductAttribute;

public sealed record CreateProductAttributeCommand(
    string Name,
    string Code,
    ProductAttributeDataType DataType,
    bool IsVariantDefining,
    bool IsFilterable) : IRequest<Guid>;
