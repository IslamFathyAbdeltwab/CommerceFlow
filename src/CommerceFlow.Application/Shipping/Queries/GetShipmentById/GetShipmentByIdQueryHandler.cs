using CommerceFlow.Application.Shipping.Interfaces;
using MediatR;

namespace CommerceFlow.Application.Shipping.Queries.GetShipmentById;

public sealed class GetShipmentByIdQueryHandler(IShipmentRepository shipments)
    : IRequestHandler<GetShipmentByIdQuery, ShipmentDto?>
{
    public async Task<ShipmentDto?> Handle(GetShipmentByIdQuery request, CancellationToken cancellationToken)
    {
        var shipment = await shipments.GetByIdAsync(request.ShipmentId, cancellationToken);

        return shipment is null ? null : ShipmentMappings.ToDto(shipment);
    }
}
