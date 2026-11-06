using CommerceFlow.Domain.Enums;

namespace CommerceFlow.Application.Catalog.ProductAttributes.Queries;

public sealed record ProductAttributeDto(
    Guid Id,
    string Name,
    string Code,
    ProductAttributeDataType DataType,
    bool IsVariantDefining,
    bool IsFilterable,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record ProductAttributeListItemDto(
    Guid Id,
    string Name,
    string Code,
    ProductAttributeDataType DataType,
    bool IsVariantDefining,
    bool IsFilterable);
