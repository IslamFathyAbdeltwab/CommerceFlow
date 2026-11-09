using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Inventory.Interfaces;
using CommerceFlow.Domain.Entities.Inventory;
using MediatR;

namespace CommerceFlow.Application.Inventory.InventoryItems.Commands.ReserveStock;

public sealed class ReserveStockCommandHandler(
    IInventoryItemRepository items,
    IUnitOfWork unitOfWork) : IRequestHandler<ReserveStockCommand>
{
    public async Task Handle(ReserveStockCommand request, CancellationToken cancellationToken)
    {
        var item = await items.GetByIdAsync(request.InventoryItemId, cancellationToken)
            ?? throw new NotFoundException(nameof(InventoryItem), request.InventoryItemId);

        item.Reserve(request.Quantity);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
