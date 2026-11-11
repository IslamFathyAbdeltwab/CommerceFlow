using CommerceFlow.Application.Carts.Interfaces;
using CommerceFlow.Domain.Entities.Carts;
using CommerceFlow.Domain.Enums;
using CommerceFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommerceFlow.Infrastructure.Carts;

internal sealed class CartRepository(CommerceFlowDbContext dbContext) : ICartRepository
{
    public async Task<Cart?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.Carts
            .Include(c => c.Lines)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<Cart?> GetActiveByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
        => await dbContext.Carts
            .Include(c => c.Lines)
            .FirstOrDefaultAsync(
                c => c.CustomerId == customerId && c.Status == CartStatus.Active,
                cancellationToken);

    public async Task<IReadOnlyList<Cart>> GetAllAsync(
        Guid? customerId,
        CartStatus? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
        // Lines are included because list items expose derived LineCount/TotalQuantity.
        => await ApplyFilters(dbContext.Carts.AsNoTracking().Include(c => c.Lines), customerId, status)
            .OrderByDescending(c => c.UpdatedAtUtc) // most recently touched carts first
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

    public async Task<int> CountAsync(
        Guid? customerId,
        CartStatus? status,
        CancellationToken cancellationToken = default)
        => await ApplyFilters(dbContext.Carts.AsNoTracking(), customerId, status)
            .CountAsync(cancellationToken);

    public async Task AddAsync(Cart cart, CancellationToken cancellationToken = default)
        => await dbContext.Carts.AddAsync(cart, cancellationToken);

    private static IQueryable<Cart> ApplyFilters(
        IQueryable<Cart> query,
        Guid? customerId,
        CartStatus? status)
    {
        if (customerId.HasValue)
            query = query.Where(c => c.CustomerId == customerId.Value);

        if (status.HasValue)
            query = query.Where(c => c.Status == status.Value);

        return query;
    }
}
