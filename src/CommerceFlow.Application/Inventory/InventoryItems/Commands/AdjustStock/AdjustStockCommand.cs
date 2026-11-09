using MediatR;

namespace CommerceFlow.Application.Inventory.InventoryItems.Commands.AdjustStock;

public sealed record AdjustStockCommand(
    Guid InventoryItemId,
    int QuantityChange,
    string Reason) : IRequest;
