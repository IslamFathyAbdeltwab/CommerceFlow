using CommerceFlow.Application.Returns.Interfaces;
using CommerceFlow.Domain.Entities.Returns;
using CommerceFlow.Domain.Enums;
using CommerceFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommerceFlow.Infrastructure.Returns;

internal sealed class ReturnRequestRepository(CommerceFlowDbContext dbContext)
    : IReturnRequestRepository
{
    public async Task<ReturnRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        // Items always ride along: the detail read maps them and MarkReceived
        // counts the basket - an unloaded collection would look empty.
        => await dbContext.ReturnRequests
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ReturnRequest>> GetByOrderIdAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
        // Items included: the create handler sums them per variant for the
        // returnable-quantity pre-check across multiple return requests.
        => await dbContext.ReturnRequests
            .Include(r => r.Items)
            .Where(r => r.OrderId == orderId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ReturnRequest>> GetAllAsync(
        Guid? orderId,
        Guid? customerId,
        ReturnStatus? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
        => await ApplyFilters(
                dbContext.ReturnRequests.AsNoTracking().Include(r => r.Items),
                orderId, customerId, status)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ThenByDescending(r => r.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

    public async Task<int> CountAsync(
        Guid? orderId,
        Guid? customerId,
        ReturnStatus? status,
        CancellationToken cancellationToken = default)
        => await ApplyFilters(dbContext.ReturnRequests.AsNoTracking(), orderId, customerId, status)
            .CountAsync(cancellationToken);

    public async Task AddAsync(ReturnRequest returnRequest, CancellationToken cancellationToken = default)
        => await dbContext.ReturnRequests.AddAsync(returnRequest, cancellationToken);

    private static IQueryable<ReturnRequest> ApplyFilters(
        IQueryable<ReturnRequest> query,
        Guid? orderId,
        Guid? customerId,
        ReturnStatus? status)
    {
        if (orderId.HasValue)
            query = query.Where(r => r.OrderId == orderId.Value);

        if (customerId.HasValue)
            query = query.Where(r => r.CustomerId == customerId.Value);

        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        return query;
    }
}
