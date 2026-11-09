using CommerceFlow.Domain.Enums;

namespace CommerceFlow.Application.Inventory.Reservations.Queries;

public sealed record InventoryReservationDto(
    Guid Id,
    Guid OrderId,
    InventoryReservationStatus Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    DateTimeOffset? ReleasedAtUtc,
    IReadOnlyList<InventoryReservationItemDto> Items);

public sealed record InventoryReservationItemDto(
    Guid Id,
    Guid WarehouseId,
    Guid ProductVariantId,
    int Quantity);

public sealed record InventoryReservationListItemDto(
    Guid Id,
    Guid OrderId,
    InventoryReservationStatus Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ExpiresAtUtc);
