using CommerceFlow.Domain.Entities.Notifications;
using CommerceFlow.Domain.Enums;

namespace CommerceFlow.Application.Notifications.Interfaces;

public interface INotificationRepository
{
    Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    // Feed filter: by recipient ("my notifications") and/or delivery state
    // (admin view of what is stuck), with the usual paging.
    Task<IReadOnlyList<Notification>> GetAllAsync(
        Guid? recipientId,
        NotificationStatus? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(
        Guid? recipientId,
        NotificationStatus? status,
        CancellationToken cancellationToken = default);

    Task AddAsync(Notification notification, CancellationToken cancellationToken = default);

    // No Delete: notifications are history (read or failed, they stay);
    // updates ride EF change tracking, like the other repositories.
}
