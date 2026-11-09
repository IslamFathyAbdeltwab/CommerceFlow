using MediatR;

namespace CommerceFlow.Application.Inventory.InventoryItems.Commands.CreateInventoryItem;

public sealed record CreateInventoryItemCommand(
    Guid WarehouseId,
    Guid ProductVariantId,
    int OnHandQuantity,
    int ReorderThreshold = 0) : IRequest<Guid>;
