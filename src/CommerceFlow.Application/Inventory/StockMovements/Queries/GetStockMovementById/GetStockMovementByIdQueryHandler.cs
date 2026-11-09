using CommerceFlow.Application.Inventory.Interfaces;
using MediatR;

namespace CommerceFlow.Application.Inventory.StockMovements.Queries.GetStockMovementById;

public sealed class GetStockMovementByIdQueryHandler(
    IStockMovementRepository movements) : IRequestHandler<GetStockMovementByIdQuery, StockMovementDto?>
{
    public async Task<StockMovementDto?> Handle(GetStockMovementByIdQuery request, CancellationToken cancellationToken)
    {
        var movement = await movements.GetByIdAsync(request.MovementId, cancellationToken);
        if (movement is null)
            return null;

        return new StockMovementDto(
            movement.Id,
            movement.WarehouseId,
            movement.ProductVariantId,
            movement.MovementType,
            movement.QuantityChange,
            movement.QuantityAfter,
            movement.ReferenceType,
            movement.ReferenceId,
            movement.Reason,
            movement.PerformedByUserId,
            movement.CreatedAtUtc);
    }
}
