using CommerceFlow.Application.Inventory.Interfaces;
using MediatR;

namespace CommerceFlow.Application.Inventory.Reservations.Queries.GetInventoryReservationById;

public sealed class GetInventoryReservationByIdQueryHandler(
    IInventoryReservationRepository reservations) : IRequestHandler<GetInventoryReservationByIdQuery, InventoryReservationDto?>
{
    public async Task<InventoryReservationDto?> Handle(GetInventoryReservationByIdQuery request, CancellationToken cancellationToken)
    {
        var reservation = await reservations.GetByIdAsync(request.ReservationId, cancellationToken);
        if (reservation is null)
            return null;

        return new InventoryReservationDto(
            reservation.Id,
            reservation.OrderId,
            reservation.Status,
            reservation.CreatedAtUtc,
            reservation.ExpiresAtUtc,
            reservation.ReleasedAtUtc,
            reservation.Items.Select(i => new InventoryReservationItemDto(i.Id, i.WarehouseId, i.ProductVariantId, i.Quantity)).ToList());
    }
}
