using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Inventory.Interfaces;
using CommerceFlow.Domain.Entities.Inventory;
using MediatR;

namespace CommerceFlow.Application.Inventory.InventoryItems.Commands.ReleaseReservation;

public sealed class ReleaseReservationCommandHandler(
    IInventoryItemRepository items,
    IUnitOfWork unitOfWork) : IRequestHandler<ReleaseReservationCommand>
{
    public async Task Handle(ReleaseReservationCommand request, CancellationToken cancellationToken)
    {
        var item = await items.GetByIdAsync(request.InventoryItemId, cancellationToken)
            ?? throw new NotFoundException(nameof(InventoryItem), request.InventoryItemId);

        item.ReleaseReservation(request.Quantity);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
