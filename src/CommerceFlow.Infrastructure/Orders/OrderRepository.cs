using CommerceFlow.Application.Orders.Interfaces;
using CommerceFlow.Domain.Entities.Orders;
using CommerceFlow.Domain.Enums;
using CommerceFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommerceFlow.Infrastructure.Orders;

internal sealed class OrderRepository(CommerceFlowDbContext dbContext) : IOrderRepository
{
    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.Orders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Order>> GetAllAsync(
        Guid? customerId,
        OrderStatus? status,
        OrderPaymentStatus? paymentStatus,
        string? searchTerm,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
        => await ApplyFilters(dbContext.Orders.AsNoTracking(), customerId, status, paymentStatus, searchTerm)
            .OrderByDescending(o => o.CreatedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

    public async Task<int> CountAsync(
        Guid? customerId,
        OrderStatus? status,
        OrderPaymentStatus? paymentStatus,
        string? searchTerm,
        CancellationToken cancellationToken = default)
        => await ApplyFilters(dbContext.Orders.AsNoTracking(), customerId, status, paymentStatus, searchTerm)
            .CountAsync(cancellationToken);

    public async Task AddAsync(Order order, CancellationToken cancellationToken = default)
        => await dbContext.Orders.AddAsync(order, cancellationToken);

    private static IQueryable<Order> ApplyFilters(
        IQueryable<Order> query,
        Guid? customerId,
        OrderStatus? status,
        OrderPaymentStatus? paymentStatus,
        string? searchTerm)
    {
        if (customerId.HasValue)
            query = query.Where(o => o.CustomerId == customerId.Value);

        if (status.HasValue)
            query = query.Where(o => o.Status == status.Value);

        if (paymentStatus.HasValue)
            query = query.Where(o => o.PaymentStatus == paymentStatus.Value);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            query = query.Where(o => o.OrderNumber.Contains(term));
        }

        return query;
    }
}
