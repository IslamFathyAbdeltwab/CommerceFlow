using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Inventory.Interfaces;
using CommerceFlow.Domain.Entities.Inventory;
using MediatR;

namespace CommerceFlow.Application.Inventory.InventoryItems.Commands.ConsumeReservation;

public sealed class ConsumeReservationCommandHandler(
    IInventoryItemRepository items,
    IUnitOfWork unitOfWork) : IRequestHandler<ConsumeReservationCommand>
{
    public async Task Handle(ConsumeReservationCommand request, CancellationToken cancellationToken)
    {
        var item = await items.GetByIdAsync(request.InventoryItemId, cancellationToken)
            ?? throw new NotFoundException(nameof(InventoryItem), request.InventoryItemId);

        item.ConsumeReservation(request.Quantity);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
