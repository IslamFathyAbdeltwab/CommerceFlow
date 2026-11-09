using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Inventory.Interfaces;
using CommerceFlow.Domain.Entities.Inventory;
using MediatR;

namespace CommerceFlow.Application.Inventory.InventoryItems.Commands.CreateInventoryItem;

public sealed class CreateInventoryItemCommandHandler(
    IInventoryItemRepository items,
    IWarehouseRepository warehouses,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateInventoryItemCommand, Guid>
{
    public async Task<Guid> Handle(CreateInventoryItemCommand request, CancellationToken cancellationToken)
    {
        if (!await warehouses.ExistsAsync(request.WarehouseId, cancellationToken))
            throw new NotFoundException("Warehouse", request.WarehouseId);

        // NOTE: Variant existence is enforced by Infrastructure FK + Catalog validation later.
        // Application must not call GetProductByIdAsync with a variantId (different aggregate).

        if (await items.ExistsAsync(request.WarehouseId, request.ProductVariantId, cancellationToken))
            throw new ConflictException("Inventory item for this warehouse and variant already exists.");

        var item = InventoryItem.Create(
            request.WarehouseId,
            request.ProductVariantId,
            request.OnHandQuantity,
            request.ReorderThreshold);

        await items.AddAsync(item, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return item.Id;
    }
}
