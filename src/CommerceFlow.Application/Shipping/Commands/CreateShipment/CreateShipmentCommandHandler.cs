using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Orders.Interfaces;
using CommerceFlow.Application.Shipping.Interfaces;
using CommerceFlow.Domain.Entities.Orders;
using CommerceFlow.Domain.Entities.Shipping;
using CommerceFlow.Domain.Enums;
using MediatR;

namespace CommerceFlow.Application.Shipping.Commands.CreateShipment;

public sealed class CreateShipmentCommandHandler(
    IShipmentRepository shipments,
    IOrderRepository orders,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateShipmentCommand, Guid>
{
    public async Task<Guid> Handle(CreateShipmentCommand request, CancellationToken cancellationToken)
    {
        var trackingNumber = request.TrackingNumber.Trim();

        var order = await orders.GetByIdAsync(request.OrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);

        // pre-checks before anything is created (state conflicts -> 409)
        if (order.Status is not (OrderStatus.Confirmed or OrderStatus.Shipped))
            throw new ConflictException(
                $"Only confirmed orders can be shipped (current: {order.Status}).");

        // unique tracking number: a duplicate would make carrier lookups ambiguous
        if (await shipments.GetByTrackingNumberAsync(trackingNumber, cancellationToken) is not null)
            throw new ConflictException("Tracking number is already in use.");

        var existing = await shipments.GetByOrderIdAsync(order.Id, cancellationToken);

        var shipment = Shipment.Create(
            order.Id,
            request.Carrier.Trim(),
            trackingNumber,
            request.ServiceLevel);

        foreach (var item in request.Items)
        {
            var line = order.Lines.FirstOrDefault(l => l.ProductVariantId == item.ProductVariantId)
                ?? throw new ConflictException(
                    "Shipment item does not match any order line.");

            // partial shipments: this box may only carry what the order still
            // owes - Cancelled/Returned boxes released their claim on the qty
            var alreadyShipped = existing
                .Where(s => s.Status is not (ShipmentStatus.Cancelled or ShipmentStatus.Returned))
                .SelectMany(s => s.Items)
                .Where(i => i.ProductVariantId == item.ProductVariantId)
                .Sum(i => i.Quantity);

            if (item.Quantity > line.Quantity - alreadyShipped)
                throw new ConflictException(
                    "Shipment quantity exceeds the unshipped quantity.");

            shipment.AddItem(item.ProductVariantId, item.Quantity);
        }

        await shipments.AddAsync(shipment, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return shipment.Id;
    }
}
