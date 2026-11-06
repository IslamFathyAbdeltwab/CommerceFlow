using CommerceFlow.Application.Catalog.Products.Queries;

namespace CommerceFlow.Application.Catalog.Brands.Queries;

public sealed record BrandListItemDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string? LogoUrl,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record BrandDetailDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string? LogoUrl,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
