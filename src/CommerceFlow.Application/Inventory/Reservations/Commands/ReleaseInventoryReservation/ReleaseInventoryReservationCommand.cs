using MediatR;

namespace CommerceFlow.Application.Inventory.Reservations.Commands.ReleaseInventoryReservation;

public sealed record ReleaseInventoryReservationCommand(Guid ReservationId) : IRequest;
