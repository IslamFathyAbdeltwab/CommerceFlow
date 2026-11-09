using MediatR;

namespace CommerceFlow.Application.Inventory.InventoryItems.Commands.ConsumeReservation;

public sealed record ConsumeReservationCommand(
    Guid InventoryItemId,
    int Quantity) : IRequest;
