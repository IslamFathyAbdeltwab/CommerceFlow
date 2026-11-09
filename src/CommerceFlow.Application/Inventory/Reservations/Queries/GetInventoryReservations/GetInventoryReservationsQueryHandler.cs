using CommerceFlow.Application.Catalog.Products.Queries;
using CommerceFlow.Application.Inventory.Interfaces;
using MediatR;

namespace CommerceFlow.Application.Inventory.Reservations.Queries.GetInventoryReservations;

public sealed class GetInventoryReservationsQueryHandler(
    IInventoryReservationRepository reservations) : IRequestHandler<GetInventoryReservationsQuery, PagedResult<InventoryReservationListItemDto>>
{
    public async Task<PagedResult<InventoryReservationListItemDto>> Handle(GetInventoryReservationsQuery request, CancellationToken cancellationToken)
    {
        var totalCount = await reservations.CountAsync(request.OrderId, request.Status, cancellationToken);

        var list = await reservations.GetAllAsync(request.OrderId, request.Status, request.PageNumber, request.PageSize, cancellationToken);

        var items = list.Select(r => new InventoryReservationListItemDto(
            r.Id,
            r.OrderId,
            r.Status,
            r.CreatedAtUtc,
            r.ExpiresAtUtc)).ToList();

        return new PagedResult<InventoryReservationListItemDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}
