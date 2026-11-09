using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Inventory.Interfaces;
using CommerceFlow.Domain.Entities.Inventory;
using MediatR;

namespace CommerceFlow.Application.Inventory.InventoryItems.Commands.SetReorderThreshold;

public sealed class SetReorderThresholdCommandHandler(
    IInventoryItemRepository items,
    IUnitOfWork unitOfWork) : IRequestHandler<SetReorderThresholdCommand>
{
    public async Task Handle(SetReorderThresholdCommand request, CancellationToken cancellationToken)
    {
        var item = await items.GetByIdAsync(request.InventoryItemId, cancellationToken)
            ?? throw new NotFoundException(nameof(InventoryItem), request.InventoryItemId);

        item.SetReorderThreshold(request.Threshold);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
