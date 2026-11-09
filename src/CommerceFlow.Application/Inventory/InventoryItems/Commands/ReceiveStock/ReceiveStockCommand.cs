using MediatR;

namespace CommerceFlow.Application.Inventory.InventoryItems.Commands.ReceiveStock;

public sealed record ReceiveStockCommand(
    Guid InventoryItemId,
    int Quantity,
    string Reason) : IRequest;
