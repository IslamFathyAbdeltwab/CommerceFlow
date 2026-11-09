using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Inventory.Interfaces;
using CommerceFlow.Domain.Entities.Inventory;
using MediatR;

namespace CommerceFlow.Application.Inventory.Reservations.Commands.CreateInventoryReservation;

public sealed class CreateInventoryReservationCommandHandler(
    IInventoryReservationRepository reservations,
    IInventoryItemRepository items,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateInventoryReservationCommand, Guid>
{
    public async Task<Guid> Handle(CreateInventoryReservationCommand request, CancellationToken cancellationToken)
    {
        if (request.Items is null || request.Items.Count == 0)
            throw new ConflictException("Reservation must contain at least one item.");

        if (await reservations.GetByOrderIdAsync(request.OrderId, cancellationToken) is not null)
            throw new ConflictException("An inventory reservation for this order already exists.");

        var reservation = InventoryReservation.Create(request.OrderId, request.ExpiresAtUtc);

        foreach (var itemDto in request.Items)
        {
            var inventoryItem = await items.GetByWarehouseAndVariantAsync(itemDto.WarehouseId, itemDto.ProductVariantId, cancellationToken)
                ?? throw new NotFoundException("InventoryItem", $"{itemDto.WarehouseId}/{itemDto.ProductVariantId}");

            if (itemDto.Quantity > inventoryItem.AvailableQuantity)
                throw new ConflictException("Cannot reserve more than available quantity.");

            inventoryItem.Reserve(itemDto.Quantity);
            reservation.AddItem(itemDto.WarehouseId, itemDto.ProductVariantId, itemDto.Quantity);
        }

        await reservations.AddAsync(reservation, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return reservation.Id;
    }
}
