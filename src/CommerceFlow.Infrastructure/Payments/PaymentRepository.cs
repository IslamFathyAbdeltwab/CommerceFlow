using CommerceFlow.Application.Payments.Interfaces;
using CommerceFlow.Domain.Entities.Payments;
using CommerceFlow.Domain.Enums;
using CommerceFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommerceFlow.Infrastructure.Payments;

internal sealed class PaymentRepository(CommerceFlowDbContext dbContext)
    : IPaymentRepository
{
    public async Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.Payments
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<Payment?> GetByIdempotencyKeyAsync(
        Guid orderId,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
        // Backed by the unique (OrderId, IdempotencyKey) index: this lookup is
        // the create-on-retry path - same key returns the original payment.
        => await dbContext.Payments
            .FirstOrDefaultAsync(
                p => p.OrderId == orderId && p.IdempotencyKey == idempotencyKey,
                cancellationToken);

    public async Task<IReadOnlyList<Payment>> GetAllAsync(
        Guid? orderId,
        Guid? customerId,
        PaymentStatus? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
        => await ApplyFilters(dbContext.Payments.AsNoTracking(), orderId, customerId, status)
            .OrderByDescending(p => p.CreatedAtUtc)
            .ThenByDescending(p => p.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

    public async Task<int> CountAsync(
        Guid? orderId,
        Guid? customerId,
        PaymentStatus? status,
        CancellationToken cancellationToken = default)
        => await ApplyFilters(dbContext.Payments.AsNoTracking(), orderId, customerId, status)
            .CountAsync(cancellationToken);

    public async Task AddAsync(Payment payment, CancellationToken cancellationToken = default)
        => await dbContext.Payments.AddAsync(payment, cancellationToken);

    private static IQueryable<Payment> ApplyFilters(
        IQueryable<Payment> query,
        Guid? orderId,
        Guid? customerId,
        PaymentStatus? status)
    {
        if (orderId.HasValue)
            query = query.Where(p => p.OrderId == orderId.Value);

        if (customerId.HasValue)
            query = query.Where(p => p.CustomerId == customerId.Value);

        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);

        return query;
    }
}
