using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Orders.Interfaces;
using CommerceFlow.Application.Shipping.Interfaces;
using CommerceFlow.Domain.Entities.Orders;
using CommerceFlow.Domain.Entities.Shipping;
using CommerceFlow.Domain.Enums;
using MediatR;

namespace CommerceFlow.Application.Shipping.Commands.ShipShipment;

public sealed class ShipShipmentCommandHandler(
    IShipmentRepository shipments,
    IOrderRepository orders,
    IUnitOfWork unitOfWork) : IRequestHandler<ShipShipmentCommand>
{
    public async Task Handle(ShipShipmentCommand request, CancellationToken cancellationToken)
    {
        var shipment = await shipments.GetByIdAsync(request.ShipmentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Shipment), request.ShipmentId);

        // pre-checks BEFORE the hand-off (state conflicts -> 409)
        if (shipment.Status != ShipmentStatus.Pending)
            throw new ConflictException(
                $"Only pending shipments can be shipped (current: {shipment.Status}).");

        var order = await orders.GetByIdAsync(shipment.OrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), shipment.OrderId);

        if (order.Status is not (OrderStatus.Confirmed or OrderStatus.Shipped))
            throw new ConflictException(
                $"Only confirmed orders can be shipped (current: {order.Status}).");

        shipment.MarkShipped();

        // cross-module sync through IOrderRepository: the first shipment
        // hands the order over; later (partial) shipments leave it alone
        if (order.Status == OrderStatus.Confirmed)
            order.Ship();

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
