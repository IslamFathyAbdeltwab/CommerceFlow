using CommerceFlow.Application.Inventory.Interfaces;
using CommerceFlow.Domain.Entities.Inventory;
using CommerceFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommerceFlow.Infrastructure.Inventory.Warehouses;

internal sealed class WarehouseRepository(CommerceFlowDbContext dbContext) : IWarehouseRepository
{
    public async Task<Warehouse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.Warehouses.FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

    public async Task<Warehouse?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
        => await dbContext.Warehouses.AsNoTracking().FirstOrDefaultAsync(w => w.Code == code, cancellationToken);

    public async Task<IReadOnlyList<Warehouse>> GetAllAsync(
        string? searchTerm,
        bool? isActive,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
        => await ApplyFilters(dbContext.Warehouses.AsNoTracking(), searchTerm, isActive)
            .OrderBy(w => w.Code)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

    public async Task<int> CountAsync(
        string? searchTerm,
        bool? isActive,
        CancellationToken cancellationToken = default)
        => await ApplyFilters(dbContext.Warehouses.AsNoTracking(), searchTerm, isActive)
            .CountAsync(cancellationToken);

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.Warehouses.AsNoTracking().AnyAsync(w => w.Id == id, cancellationToken);

    public async Task<bool> CodeExistsAsync(string code, Guid? excludeWarehouseId = null, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Warehouses.AsNoTracking().Where(w => w.Code == code);
        if (excludeWarehouseId.HasValue)
            query = query.Where(w => w.Id != excludeWarehouseId.Value);
        return await query.AnyAsync(cancellationToken);
    }

    public async Task AddAsync(Warehouse warehouse, CancellationToken cancellationToken = default)
        => await dbContext.Warehouses.AddAsync(warehouse, cancellationToken);

    private static IQueryable<Warehouse> ApplyFilters(
        IQueryable<Warehouse> query,
        string? searchTerm,
        bool? isActive)
    {
        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(w => w.Code.Contains(searchTerm) || w.Name.Contains(searchTerm));

        if (isActive.HasValue)
            query = query.Where(w => w.IsActive == isActive.Value);

        return query;
    }
}
