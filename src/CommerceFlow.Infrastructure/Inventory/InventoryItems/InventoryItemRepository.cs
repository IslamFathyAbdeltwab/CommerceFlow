using CommerceFlow.Application.Inventory.Interfaces;
using CommerceFlow.Domain.Entities.Inventory;
using CommerceFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommerceFlow.Infrastructure.Inventory.InventoryItems;

internal sealed class InventoryItemRepository(CommerceFlowDbContext dbContext) : IInventoryItemRepository
{
    public async Task<InventoryItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.InventoryItems.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public async Task<InventoryItem?> GetByWarehouseAndVariantAsync(Guid warehouseId, Guid productVariantId, CancellationToken cancellationToken = default)
        => await dbContext.InventoryItems
            .FirstOrDefaultAsync(i => i.WarehouseId == warehouseId && i.ProductVariantId == productVariantId, cancellationToken);

    public async Task<IReadOnlyList<InventoryItem>> GetAllAsync(
        Guid? warehouseId,
        Guid? productVariantId,
        bool? lowStockOnly,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
        => await ApplyFilters(dbContext.InventoryItems.AsNoTracking(), warehouseId, productVariantId, lowStockOnly)
            .OrderBy(i => i.WarehouseId)
            .ThenBy(i => i.ProductVariantId)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

    public async Task<int> CountAsync(
        Guid? warehouseId,
        Guid? productVariantId,
        bool? lowStockOnly,
        CancellationToken cancellationToken = default)
        => await ApplyFilters(dbContext.InventoryItems.AsNoTracking(), warehouseId, productVariantId, lowStockOnly)
            .CountAsync(cancellationToken);

    public async Task<bool> ExistsAsync(Guid warehouseId, Guid productVariantId, CancellationToken cancellationToken = default)
        => await dbContext.InventoryItems.AsNoTracking()
            .AnyAsync(i => i.WarehouseId == warehouseId && i.ProductVariantId == productVariantId, cancellationToken);

    public async Task AddAsync(InventoryItem item, CancellationToken cancellationToken = default)
        => await dbContext.InventoryItems.AddAsync(item, cancellationToken);

    private static IQueryable<InventoryItem> ApplyFilters(
        IQueryable<InventoryItem> query,
        Guid? warehouseId,
        Guid? productVariantId,
        bool? lowStockOnly)
    {
        if (warehouseId.HasValue)
            query = query.Where(i => i.WarehouseId == warehouseId.Value);

        if (productVariantId.HasValue)
            query = query.Where(i => i.ProductVariantId == productVariantId.Value);

        if (lowStockOnly is true)
            query = query.Where(i => i.OnHandQuantity - i.ReservedQuantity <= i.ReorderThreshold);

        return query;
    }
}
