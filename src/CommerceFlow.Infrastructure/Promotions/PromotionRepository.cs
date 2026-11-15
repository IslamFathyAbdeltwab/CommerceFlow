using CommerceFlow.Application.Promotions.Interfaces;
using CommerceFlow.Domain.Entities.Promotions;
using CommerceFlow.Domain.Enums;
using CommerceFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommerceFlow.Infrastructure.Promotions;

internal sealed class PromotionRepository(CommerceFlowDbContext dbContext)
    : IPromotionRepository
{
    public async Task<Promotion?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        // Usages always ride along: the domain counts per-customer
        // redemptions from that collection when enforcing limits - an
        // unloaded collection would look like zero uses (same reason
        // ReturnRequestRepository includes Items).
        => await dbContext.Promotions
            .Include(p => p.Usages)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<Promotion?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
        // Case-insensitive on purpose: the unique index runs on the
        // case-insensitive collation, so the pre-check must not disagree
        // with it (SAVE10 vs save10 is one campaign).
        => await dbContext.Promotions
            .AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.Code.ToLower() == code.ToLower(), cancellationToken);

    public async Task<IReadOnlyList<Promotion>> GetAllAsync(
        string? code,
        PromotionStatus? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
        // Usages not included: the list DTO only exposes CurrentUses.
        => await ApplyFilters(dbContext.Promotions.AsNoTracking(), code, status)
            .OrderByDescending(p => p.CreatedAtUtc)
            .ThenByDescending(p => p.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

    public async Task<int> CountAsync(
        string? code,
        PromotionStatus? status,
        CancellationToken cancellationToken = default)
        => await ApplyFilters(dbContext.Promotions.AsNoTracking(), code, status)
            .CountAsync(cancellationToken);

    public async Task AddAsync(Promotion promotion, CancellationToken cancellationToken = default)
        => await dbContext.Promotions.AddAsync(promotion, cancellationToken);

    private static IQueryable<Promotion> ApplyFilters(
        IQueryable<Promotion> query,
        string? code,
        PromotionStatus? status)
    {
        if (!string.IsNullOrWhiteSpace(code))
        {
            var needle = code.Trim().ToLower();
            query = query.Where(p => p.Code.ToLower().Contains(needle));
        }

        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);

        return query;
    }
}
