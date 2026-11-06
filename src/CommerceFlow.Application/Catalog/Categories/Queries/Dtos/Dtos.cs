namespace CommerceFlow.Application.Catalog.Categories.Queries;

public sealed record CategoryListItemDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    Guid? ParentCategoryId,
    bool IsActive,
    int DisplayOrder,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record CategoryDetailDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    Guid? ParentCategoryId,
    bool IsActive,
    int DisplayOrder,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record CategoryTreeDto(
    Guid Id,
    string Name,
    string Slug,
    int DisplayOrder,
    IReadOnlyList<CategoryTreeDto> Children);
