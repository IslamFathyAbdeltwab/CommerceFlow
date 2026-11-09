using MediatR;

namespace CommerceFlow.Application.Inventory.Reservations.Commands.CreateInventoryReservation;

public sealed record CreateInventoryReservationCommand(
    Guid OrderId,
    IReadOnlyList<CreateInventoryReservationItemDto> Items,
    DateTimeOffset? ExpiresAtUtc = null) : IRequest<Guid>;

public sealed record CreateInventoryReservationItemDto(
    Guid WarehouseId,
    Guid ProductVariantId,
    int Quantity);
