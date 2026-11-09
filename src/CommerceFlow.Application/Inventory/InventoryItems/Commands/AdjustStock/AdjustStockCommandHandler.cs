using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Inventory.Interfaces;
using CommerceFlow.Domain.Entities.Inventory;
using CommerceFlow.Domain.Enums;
using MediatR;

namespace CommerceFlow.Application.Inventory.InventoryItems.Commands.AdjustStock;

public sealed class AdjustStockCommandHandler(
    IInventoryItemRepository items,
    IStockMovementRepository movements,
    IUnitOfWork unitOfWork) : IRequestHandler<AdjustStockCommand>
{
    public async Task Handle(AdjustStockCommand request, CancellationToken cancellationToken)
    {
        var item = await items.GetByIdAsync(request.InventoryItemId, cancellationToken)
            ?? throw new NotFoundException(nameof(InventoryItem), request.InventoryItemId);

        item.AdjustStock(request.QuantityChange, request.Reason);

        var movement = StockMovement.Create(
            item.WarehouseId,
            item.ProductVariantId,
            StockMovementType.ManualAdjustment,
            request.QuantityChange,
            item.OnHandQuantity,
            request.Reason);

        await movements.AddAsync(movement, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
