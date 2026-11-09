namespace CommerceFlow.Application.Inventory.InventoryItems.Queries;

public sealed record InventoryItemDto(
    Guid Id,
    Guid WarehouseId,
    Guid ProductVariantId,
    int OnHandQuantity,
    int ReservedQuantity,
    int AvailableQuantity,
    int ReorderThreshold,
    bool IsBelowReorderThreshold,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record InventoryItemListItemDto(
    Guid Id,
    Guid WarehouseId,
    Guid ProductVariantId,
    int OnHandQuantity,
    int ReservedQuantity,
    int AvailableQuantity,
    bool IsBelowReorderThreshold);
