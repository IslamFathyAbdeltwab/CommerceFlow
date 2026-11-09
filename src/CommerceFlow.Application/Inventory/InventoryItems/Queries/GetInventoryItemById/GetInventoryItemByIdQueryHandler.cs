using CommerceFlow.Application.Inventory.Interfaces;
using MediatR;

namespace CommerceFlow.Application.Inventory.InventoryItems.Queries.GetInventoryItemById;

public sealed class GetInventoryItemByIdQueryHandler(
    IInventoryItemRepository items) : IRequestHandler<GetInventoryItemByIdQuery, InventoryItemDto?>
{
    public async Task<InventoryItemDto?> Handle(GetInventoryItemByIdQuery request, CancellationToken cancellationToken)
    {
        var item = await items.GetByIdAsync(request.InventoryItemId, cancellationToken);
        if (item is null)
            return null;

        return new InventoryItemDto(
            item.Id,
            item.WarehouseId,
            item.ProductVariantId,
            item.OnHandQuantity,
            item.ReservedQuantity,
            item.AvailableQuantity,
            item.ReorderThreshold,
            item.IsBelowReorderThreshold,
            item.CreatedAtUtc,
            item.UpdatedAtUtc);
    }
}
