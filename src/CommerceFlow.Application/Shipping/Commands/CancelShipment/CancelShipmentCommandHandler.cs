using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Shipping.Interfaces;
using CommerceFlow.Domain.Entities.Shipping;
using CommerceFlow.Domain.Enums;
using MediatR;

namespace CommerceFlow.Application.Shipping.Commands.CancelShipment;

public sealed class CancelShipmentCommandHandler(
    IShipmentRepository shipments,
    IUnitOfWork unitOfWork) : IRequestHandler<CancelShipmentCommand>
{
    public async Task Handle(CancelShipmentCommand request, CancellationToken cancellationToken)
    {
        var shipment = await shipments.GetByIdAsync(request.ShipmentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Shipment), request.ShipmentId);

        // pre-check before the state moves (state conflicts -> 409): once the
        // carrier owns the box the only way back is a return
        if (shipment.Status != ShipmentStatus.Pending)
            throw new ConflictException(
                $"Only pending shipments can be cancelled (current: {shipment.Status}).");

        shipment.Cancel();

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
