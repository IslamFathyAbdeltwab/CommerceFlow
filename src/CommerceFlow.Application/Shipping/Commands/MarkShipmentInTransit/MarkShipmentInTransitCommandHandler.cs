using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Shipping.Interfaces;
using CommerceFlow.Domain.Entities.Shipping;
using CommerceFlow.Domain.Enums;
using MediatR;

namespace CommerceFlow.Application.Shipping.Commands.MarkShipmentInTransit;

public sealed class MarkShipmentInTransitCommandHandler(
    IShipmentRepository shipments,
    IUnitOfWork unitOfWork) : IRequestHandler<MarkShipmentInTransitCommand>
{
    public async Task Handle(MarkShipmentInTransitCommand request, CancellationToken cancellationToken)
    {
        var shipment = await shipments.GetByIdAsync(request.ShipmentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Shipment), request.ShipmentId);

        // pre-check before the state moves (state conflicts -> 409)
        if (shipment.Status != ShipmentStatus.Shipped)
            throw new ConflictException(
                $"Only shipped shipments can be marked in transit (current: {shipment.Status}).");

        shipment.MarkInTransit();

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
