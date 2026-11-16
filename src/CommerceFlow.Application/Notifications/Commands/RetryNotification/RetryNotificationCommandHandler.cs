using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Notifications.Interfaces;
using CommerceFlow.Domain.Entities.Notifications;
using CommerceFlow.Domain.Enums;
using MediatR;

namespace CommerceFlow.Application.Notifications.Commands.RetryNotification;

public sealed class RetryNotificationCommandHandler(
    INotificationRepository notifications,
    INotificationQueue queue,
    IUnitOfWork unitOfWork) : IRequestHandler<RetryNotificationCommand>
{
    public async Task Handle(RetryNotificationCommand request, CancellationToken cancellationToken)
    {
        var notification = await notifications.GetByIdAsync(request.NotificationId, cancellationToken)
            ?? throw new NotFoundException(nameof(Notification), request.NotificationId);

        // pre-check before the state moves (conflict -> 409): only a failure
        // is retried - Pending never left, Queued/Sent are already in flight
        // and Read is terminal
        if (notification.Status != NotificationStatus.Failed)
            throw new ConflictException(
                $"Only failed notifications can be retried (current: {notification.Status}).");

        notification.MarkQueued(); // Failed -> Queued, failure trail kept

        // back into the transport before commit (same order as Send)
        await queue.PublishAsync(notification, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
