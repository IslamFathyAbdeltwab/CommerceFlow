using MediatR;

namespace CommerceFlow.Application.Inventory.InventoryItems.Commands.SetReorderThreshold;

public sealed record SetReorderThresholdCommand(
    Guid InventoryItemId,
    int Threshold) : IRequest;
