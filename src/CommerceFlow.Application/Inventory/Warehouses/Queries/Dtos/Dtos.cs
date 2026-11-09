using CommerceFlow.Application.Catalog.Products.Queries;

namespace CommerceFlow.Application.Inventory.Warehouses.Queries;

public sealed record WarehouseDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record WarehouseListItemDto(
    Guid Id,
    string Code,
    string Name,
    bool IsActive);
