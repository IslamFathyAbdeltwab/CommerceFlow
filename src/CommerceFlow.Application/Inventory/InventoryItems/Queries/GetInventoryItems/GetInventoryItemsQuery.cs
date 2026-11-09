using CommerceFlow.Application.Catalog.Products.Queries;
using MediatR;

namespace CommerceFlow.Application.Inventory.InventoryItems.Queries.GetInventoryItems;

public sealed record GetInventoryItemsQuery(
    Guid? WarehouseId = null,
    Guid? ProductVariantId = null,
    bool? LowStockOnly = null,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PagedResult<InventoryItemListItemDto>>;
