using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Inventory.Interfaces;
using CommerceFlow.Domain.Entities.Inventory;
using CommerceFlow.Domain.Enums;
using MediatR;

namespace CommerceFlow.Application.Inventory.InventoryItems.Commands.ReceiveStock;

public sealed class ReceiveStockCommandHandler(
    IInventoryItemRepository items,
    IStockMovementRepository movements,
    IUnitOfWork unitOfWork) : IRequestHandler<ReceiveStockCommand>
{
    public async Task Handle(ReceiveStockCommand request, CancellationToken cancellationToken)
    {
        var item = await items.GetByIdAsync(request.InventoryItemId, cancellationToken)
            ?? throw new NotFoundException(nameof(InventoryItem), request.InventoryItemId);

        item.ReceiveStock(request.Quantity, request.Reason);

        var movement = StockMovement.Create(
            item.WarehouseId,
            item.ProductVariantId,
            StockMovementType.StockReceived,
            request.Quantity,
            item.OnHandQuantity,
            request.Reason);

        await movements.AddAsync(movement, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
