using CommerceFlow.Application.Catalog.Products.Queries;
using CommerceFlow.Application.Shipping.Interfaces;
using MediatR;

namespace CommerceFlow.Application.Shipping.Queries.GetShipments;

public sealed class GetShipmentsQueryHandler(IShipmentRepository shipments)
    : IRequestHandler<GetShipmentsQuery, PagedResult<ShipmentDto>>
{
    public async Task<PagedResult<ShipmentDto>> Handle(GetShipmentsQuery request, CancellationToken cancellationToken)
    {
        var totalCount = await shipments.CountAsync(
            request.OrderId,
            request.Status,
            cancellationToken);

        var list = await shipments.GetAllAsync(
            request.OrderId,
            request.Status,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var items = list.Select(ShipmentMappings.ToDto).ToList();

        return new PagedResult<ShipmentDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}
