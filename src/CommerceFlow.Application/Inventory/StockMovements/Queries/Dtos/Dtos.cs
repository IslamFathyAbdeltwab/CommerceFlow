using CommerceFlow.Domain.Enums;

namespace CommerceFlow.Application.Inventory.StockMovements.Queries;

public sealed record StockMovementDto(
    Guid Id,
    Guid WarehouseId,
    Guid ProductVariantId,
    StockMovementType MovementType,
    int QuantityChange,
    int? QuantityAfter,
    string? ReferenceType,
    Guid? ReferenceId,
    string Reason,
    Guid? PerformedByUserId,
    DateTimeOffset CreatedAtUtc);

public sealed record StockMovementListItemDto(
    Guid Id,
    Guid WarehouseId,
    Guid ProductVariantId,
    StockMovementType MovementType,
    int QuantityChange,
    DateTimeOffset CreatedAtUtc);
