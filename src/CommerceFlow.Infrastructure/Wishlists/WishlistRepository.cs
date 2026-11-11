using CommerceFlow.Application.Wishlists.Interfaces;
using CommerceFlow.Domain.Entities.Wishlists;
using CommerceFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommerceFlow.Infrastructure.Wishlists;

internal sealed class WishlistRepository(CommerceFlowDbContext dbContext) : IWishlistRepository
{
    public async Task<Wishlist?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.Wishlists
            .Include(w => w.Items)
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

    public async Task<Wishlist?> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
        => await dbContext.Wishlists
            .Include(w => w.Items)
            .FirstOrDefaultAsync(w => w.CustomerId == customerId, cancellationToken);

    public async Task<IReadOnlyList<Wishlist>> GetAllAsync(
        Guid? customerId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
        // Items are included because list items expose the derived ItemCount.
        => await ApplyFilters(dbContext.Wishlists.AsNoTracking().Include(w => w.Items), customerId)
            .OrderByDescending(w => w.UpdatedAtUtc) // most recently touched wishlists first
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

    public async Task<int> CountAsync(
        Guid? customerId,
        CancellationToken cancellationToken = default)
        => await ApplyFilters(dbContext.Wishlists.AsNoTracking(), customerId)
            .CountAsync(cancellationToken);

    public async Task AddAsync(Wishlist wishlist, CancellationToken cancellationToken = default)
        => await dbContext.Wishlists.AddAsync(wishlist, cancellationToken);

    private static IQueryable<Wishlist> ApplyFilters(
        IQueryable<Wishlist> query,
        Guid? customerId)
    {
        if (customerId.HasValue)
            query = query.Where(w => w.CustomerId == customerId.Value);

        return query;
    }
}
