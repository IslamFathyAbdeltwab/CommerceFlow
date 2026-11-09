using CommerceFlow.Domain.Entities.Inventory;
using CommerceFlow.Domain.Enums;

namespace CommerceFlow.Application.Inventory.Interfaces;

public interface IStockMovementRepository
{
    Task<StockMovement?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StockMovement>> GetAllAsync(
        Guid? warehouseId,
        Guid? productVariantId,
        StockMovementType? movementType,
        Guid? referenceId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(
        Guid? warehouseId,
        Guid? productVariantId,
        StockMovementType? movementType,
        Guid? referenceId,
        CancellationToken cancellationToken = default);

    Task AddAsync(StockMovement movement, CancellationToken cancellationToken = default);
}
