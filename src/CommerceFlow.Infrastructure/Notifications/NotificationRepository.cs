using CommerceFlow.Application.Notifications.Interfaces;
using CommerceFlow.Domain.Entities.Notifications;
using CommerceFlow.Domain.Enums;
using CommerceFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommerceFlow.Infrastructure.Notifications;

internal sealed class NotificationRepository(CommerceFlowDbContext dbContext)
    : INotificationRepository
{
    // Tracked on purpose: the confirm handlers (sent/failed/read/retry)
    // mutate what they load and save through the unit of work.
    public async Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.Notifications
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Notification>> GetAllAsync(
        Guid? recipientId,
        NotificationStatus? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
        => await ApplyFilters(dbContext.Notifications.AsNoTracking(), recipientId, status)
            .OrderByDescending(n => n.CreatedAtUtc)
            .ThenByDescending(n => n.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

    public async Task<int> CountAsync(
        Guid? recipientId,
        NotificationStatus? status,
        CancellationToken cancellationToken = default)
        => await ApplyFilters(dbContext.Notifications.AsNoTracking(), recipientId, status)
            .CountAsync(cancellationToken);

    public async Task AddAsync(Notification notification, CancellationToken cancellationToken = default)
        => await dbContext.Notifications.AddAsync(notification, cancellationToken);

    private static IQueryable<Notification> ApplyFilters(
        IQueryable<Notification> query,
        Guid? recipientId,
        NotificationStatus? status)
    {
        if (recipientId.HasValue)
            query = query.Where(n => n.RecipientId == recipientId.Value);

        if (status.HasValue)
            query = query.Where(n => n.Status == status.Value);

        return query;
    }
}
