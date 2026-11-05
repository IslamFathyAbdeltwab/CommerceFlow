using CommerceFlow.Domain.Enums;

namespace CommerceFlow.Application.Catalog.Products.Queries;

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int PageNumber,
    int PageSize)
{
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public sealed record ProductListItemDto(
    Guid Id,
    string Name,
    string Slug,
    string Description,
    Guid? BrandId,
    bool IsFeatured,
    DateTimeOffset? PublishedAtUtc,
    decimal MinPrice,
    decimal? Price,
    string? PrimaryImageStorageKey,
    IReadOnlyList<Guid> CategoryIds);

public sealed record ProductDetailDto(
    Guid Id,
    string Name,
    string Slug,
    string Description,
    Guid? BrandId,
    ProductStatus Status,
    bool IsFeatured,
    DateTimeOffset? PublishedAtUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    string? PrimaryImageStorageKey,
    IReadOnlyList<Guid> CategoryIds,
    IReadOnlyList<ProductVariantDto> Variants,
    IReadOnlyList<ProductImageDto> Images);

public sealed record ProductVariantDto(
    Guid Id,
    string Name,
    string Sku,
    decimal Price,
    decimal? CompareAtPrice,
    bool IsActive,
    IReadOnlyList<ProductVariantAttributeDto> Attributes);

public sealed record ProductVariantAttributeDto(
    Guid ProductAttributeId,
    string Value);

public sealed record ProductImageDto(
    Guid Id,
    Guid? ProductVariantId,
    string StorageKey,
    string? AltText,
    int DisplayOrder,
    bool IsPrimary);

public sealed record ProductStatsDto(
    int DraftCount,
    int PublishedCount,
    int ArchivedCount,
    int TotalCount);