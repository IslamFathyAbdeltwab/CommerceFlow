using CommerceFlow.Application.Catalog.Products.Queries;
using CommerceFlow.Application.Inventory.Interfaces;
using MediatR;

namespace CommerceFlow.Application.Inventory.InventoryItems.Queries.GetInventoryItems;

public sealed class GetInventoryItemsQueryHandler(
    IInventoryItemRepository items) : IRequestHandler<GetInventoryItemsQuery, PagedResult<InventoryItemListItemDto>>
{
    public async Task<PagedResult<InventoryItemListItemDto>> Handle(GetInventoryItemsQuery request, CancellationToken cancellationToken)
    {
        var totalCount = await items.CountAsync(
            request.WarehouseId,
            request.ProductVariantId,
            request.LowStockOnly,
            cancellationToken);

        var list = await items.GetAllAsync(
            request.WarehouseId,
            request.ProductVariantId,
            request.LowStockOnly,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var result = list.Select(i => new InventoryItemListItemDto(
            i.Id,
            i.WarehouseId,
            i.ProductVariantId,
            i.OnHandQuantity,
            i.ReservedQuantity,
            i.AvailableQuantity,
            i.IsBelowReorderThreshold)).ToList();

        return new PagedResult<InventoryItemListItemDto>(result, totalCount, request.PageNumber, request.PageSize);
    }
}
