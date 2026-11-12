using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Orders.Interfaces;
using CommerceFlow.Application.Shipping.Interfaces;
using CommerceFlow.Domain.Entities.Orders;
using CommerceFlow.Domain.Entities.Shipping;
using CommerceFlow.Domain.Enums;
using MediatR;

namespace CommerceFlow.Application.Shipping.Commands.DeliverShipment;

public sealed class DeliverShipmentCommandHandler(
    IShipmentRepository shipments,
    IOrderRepository orders,
    IUnitOfWork unitOfWork) : IRequestHandler<DeliverShipmentCommand>
{
    public async Task Handle(DeliverShipmentCommand request, CancellationToken cancellationToken)
    {
        var shipment = await shipments.GetByIdAsync(request.ShipmentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Shipment), request.ShipmentId);

        // carrier facts must always be recordable: only the shipment's own
        // transition is pre-checked, never the order's state
        if (shipment.Status is not (ShipmentStatus.Shipped or ShipmentStatus.InTransit))
            throw new ConflictException(
                $"Only shipped or in-transit shipments can be delivered (current: {shipment.Status}).");

        var order = await orders.GetByIdAsync(shipment.OrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), shipment.OrderId);

        shipment.MarkDelivered();

        // guarded sync: only advances an order that was handed over - a
        // second (partial) shipment delivering later must not redo it
        if (order.Status == OrderStatus.Shipped)
            order.Deliver();

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
