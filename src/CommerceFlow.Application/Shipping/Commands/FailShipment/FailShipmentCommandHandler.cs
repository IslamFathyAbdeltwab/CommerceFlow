using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Shipping.Interfaces;
using CommerceFlow.Domain.Entities.Shipping;
using CommerceFlow.Domain.Enums;
using MediatR;

namespace CommerceFlow.Application.Shipping.Commands.FailShipment;

public sealed class FailShipmentCommandHandler(
    IShipmentRepository shipments,
    IUnitOfWork unitOfWork) : IRequestHandler<FailShipmentCommand>
{
    public async Task Handle(FailShipmentCommand request, CancellationToken cancellationToken)
    {
        var shipment = await shipments.GetByIdAsync(request.ShipmentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Shipment), request.ShipmentId);

        // pre-check before the state moves (state conflicts -> 409)
        if (shipment.Status is not (ShipmentStatus.Shipped or ShipmentStatus.InTransit))
            throw new ConflictException(
                $"Only shipped or in-transit shipments can be marked as failed (current: {shipment.Status}).");

        shipment.MarkFailed(request.Reason.Trim());

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
