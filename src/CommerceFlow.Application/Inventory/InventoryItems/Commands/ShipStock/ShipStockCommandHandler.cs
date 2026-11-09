using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Inventory.Interfaces;
using CommerceFlow.Domain.Entities.Inventory;
using CommerceFlow.Domain.Enums;
using MediatR;

namespace CommerceFlow.Application.Inventory.InventoryItems.Commands.ShipStock;

public sealed class ShipStockCommandHandler(
    IInventoryItemRepository items,
    IStockMovementRepository movements,
    IUnitOfWork unitOfWork) : IRequestHandler<ShipStockCommand>
{
    public async Task Handle(ShipStockCommand request, CancellationToken cancellationToken)
    {
        var item = await items.GetByIdAsync(request.InventoryItemId, cancellationToken)
            ?? throw new NotFoundException(nameof(InventoryItem), request.InventoryItemId);

        item.ShipStock(request.Quantity, request.Reason);

        var movement = StockMovement.Create(
            item.WarehouseId,
            item.ProductVariantId,
            StockMovementType.Shipment,
            -request.Quantity,
            item.OnHandQuantity,
            request.Reason);

        await movements.AddAsync(movement, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
