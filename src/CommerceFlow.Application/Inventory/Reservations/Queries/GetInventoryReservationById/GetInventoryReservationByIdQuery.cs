using MediatR;

namespace CommerceFlow.Application.Inventory.Reservations.Queries.GetInventoryReservationById;

public sealed record GetInventoryReservationByIdQuery(Guid ReservationId) : IRequest<InventoryReservationDto?>;
