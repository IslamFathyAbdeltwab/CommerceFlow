using MediatR;

namespace CommerceFlow.Application.Inventory.InventoryItems.Commands.ReserveStock;

public sealed record ReserveStockCommand(
    Guid InventoryItemId,
    int Quantity) : IRequest;
