using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Shipping.Interfaces;
using CommerceFlow.Domain.Entities.Shipping;
using CommerceFlow.Domain.Enums;
using MediatR;

namespace CommerceFlow.Application.Shipping.Commands.ReturnShipment;

public sealed class ReturnShipmentCommandHandler(
    IShipmentRepository shipments,
    IUnitOfWork unitOfWork) : IRequestHandler<ReturnShipmentCommand>
{
    public async Task Handle(ReturnShipmentCommand request, CancellationToken cancellationToken)
    {
        var shipment = await shipments.GetByIdAsync(request.ShipmentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Shipment), request.ShipmentId);

        // pre-check before the state moves (state conflicts -> 409)
        if (shipment.Status != ShipmentStatus.Failed)
            throw new ConflictException(
                $"Only failed shipments can be returned to sender (current: {shipment.Status}).");

        shipment.ReturnToSender();

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
