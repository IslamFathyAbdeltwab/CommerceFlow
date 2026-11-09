using MediatR;

namespace CommerceFlow.Application.Inventory.InventoryItems.Commands.ReleaseReservation;

public sealed record ReleaseReservationCommand(
    Guid InventoryItemId,
    int Quantity) : IRequest;
