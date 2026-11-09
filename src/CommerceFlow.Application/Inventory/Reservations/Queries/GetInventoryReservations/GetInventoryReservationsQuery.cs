using CommerceFlow.Application.Catalog.Products.Queries;
using CommerceFlow.Domain.Enums;
using MediatR;

namespace CommerceFlow.Application.Inventory.Reservations.Queries.GetInventoryReservations;

public sealed record GetInventoryReservationsQuery(
    Guid? OrderId = null,
    InventoryReservationStatus? Status = null,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PagedResult<InventoryReservationListItemDto>>;
