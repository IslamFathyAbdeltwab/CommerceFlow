using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Inventory.Interfaces;
using CommerceFlow.Domain.Entities.Inventory;
using MediatR;

namespace CommerceFlow.Application.Inventory.Reservations.Commands.ConsumeInventoryReservation;

public sealed class ConsumeInventoryReservationCommandHandler(
    IInventoryReservationRepository reservations,
    IInventoryItemRepository items,
    IUnitOfWork unitOfWork) : IRequestHandler<ConsumeInventoryReservationCommand>
{
    public async Task Handle(ConsumeInventoryReservationCommand request, CancellationToken cancellationToken)
    {
        var reservation = await reservations.GetByIdAsync(request.ReservationId, cancellationToken)
            ?? throw new NotFoundException(nameof(InventoryReservation), request.ReservationId);

        foreach (var reservationItem in reservation.Items)
        {
            var inventoryItem = await items.GetByWarehouseAndVariantAsync(reservationItem.WarehouseId, reservationItem.ProductVariantId, cancellationToken)
                ?? throw new NotFoundException("InventoryItem", reservationItem.ProductVariantId);

            inventoryItem.ConsumeReservation(reservationItem.Quantity);
        }

        reservation.Consume();

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
