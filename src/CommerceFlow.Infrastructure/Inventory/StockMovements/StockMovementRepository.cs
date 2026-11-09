using CommerceFlow.Application.Inventory.Interfaces;
using CommerceFlow.Domain.Entities.Inventory;
using CommerceFlow.Domain.Enums;
using CommerceFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommerceFlow.Infrastructure.Inventory.StockMovements;

internal sealed class StockMovementRepository(CommerceFlowDbContext dbContext) : IStockMovementRepository
{
    public async Task<StockMovement?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.StockMovements.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    public async Task<IReadOnlyList<StockMovement>> GetAllAsync(
        Guid? warehouseId,
        Guid? productVariantId,
        StockMovementType? movementType,
        Guid? referenceId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
        => await ApplyFilters(dbContext.StockMovements.AsNoTracking(), warehouseId, productVariantId, movementType, referenceId)
            .OrderByDescending(m => m.CreatedAtUtcMovement)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

    public async Task<int> CountAsync(
        Guid? warehouseId,
        Guid? productVariantId,
        StockMovementType? movementType,
        Guid? referenceId,
        CancellationToken cancellationToken = default)
        => await ApplyFilters(dbContext.StockMovements.AsNoTracking(), warehouseId, productVariantId, movementType, referenceId)
            .CountAsync(cancellationToken);

    public async Task AddAsync(StockMovement movement, CancellationToken cancellationToken = default)
        => await dbContext.StockMovements.AddAsync(movement, cancellationToken);

    private static IQueryable<StockMovement> ApplyFilters(
        IQueryable<StockMovement> query,
        Guid? warehouseId,
        Guid? productVariantId,
        StockMovementType? movementType,
        Guid? referenceId)
    {
        if (warehouseId.HasValue)
            query = query.Where(m => m.WarehouseId == warehouseId.Value);

        if (productVariantId.HasValue)
            query = query.Where(m => m.ProductVariantId == productVariantId.Value);

        if (movementType.HasValue)
            query = query.Where(m => m.MovementType == movementType.Value);

        if (referenceId.HasValue)
            query = query.Where(m => m.ReferenceId == referenceId.Value);

        return query;
    }
}
