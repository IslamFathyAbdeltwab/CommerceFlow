using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Inventory.Interfaces;
using CommerceFlow.Domain.Entities.Inventory;
using MediatR;

namespace CommerceFlow.Application.Inventory.Reservations.Commands.ExpireInventoryReservation;

public sealed class ExpireInventoryReservationCommandHandler(
    IInventoryReservationRepository reservations,
    IInventoryItemRepository items,
    IUnitOfWork unitOfWork) : IRequestHandler<ExpireInventoryReservationCommand>
{
    public async Task Handle(ExpireInventoryReservationCommand request, CancellationToken cancellationToken)
    {
        var reservation = await reservations.GetByIdAsync(request.ReservationId, cancellationToken)
            ?? throw new NotFoundException(nameof(InventoryReservation), request.ReservationId);

        foreach (var reservationItem in reservation.Items)
        {
            var inventoryItem = await items.GetByWarehouseAndVariantAsync(reservationItem.WarehouseId, reservationItem.ProductVariantId, cancellationToken)
                ?? throw new NotFoundException("InventoryItem", reservationItem.ProductVariantId);

            inventoryItem.ReleaseReservation(reservationItem.Quantity);
        }

        reservation.Expire();

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
