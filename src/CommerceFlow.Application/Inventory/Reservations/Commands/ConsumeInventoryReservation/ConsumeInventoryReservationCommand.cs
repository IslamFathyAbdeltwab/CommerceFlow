using MediatR;

namespace CommerceFlow.Application.Inventory.Reservations.Commands.ConsumeInventoryReservation;

public sealed record ConsumeInventoryReservationCommand(Guid ReservationId) : IRequest;
