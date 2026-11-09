using MediatR;

namespace CommerceFlow.Application.Inventory.InventoryItems.Commands.ShipStock;

public sealed record ShipStockCommand(
    Guid InventoryItemId,
    int Quantity,
    string Reason) : IRequest;
