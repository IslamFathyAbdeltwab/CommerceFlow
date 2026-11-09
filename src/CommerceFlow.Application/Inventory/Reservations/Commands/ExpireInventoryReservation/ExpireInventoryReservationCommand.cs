using MediatR;

namespace CommerceFlow.Application.Inventory.Reservations.Commands.ExpireInventoryReservation;

public sealed record ExpireInventoryReservationCommand(Guid ReservationId) : IRequest;
