using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Notifications.Interfaces;
using CommerceFlow.Domain.Entities.Notifications;
using CommerceFlow.Domain.Enums;
using MediatR;

namespace CommerceFlow.Application.Notifications.Commands.MarkNotificationSent;

public sealed class MarkNotificationSentCommandHandler(
    INotificationRepository notifications,
    IUnitOfWork unitOfWork) : IRequestHandler<MarkNotificationSentCommand>
{
    public async Task Handle(MarkNotificationSentCommand request, CancellationToken cancellationToken)
    {
        var notification = await notifications.GetByIdAsync(request.NotificationId, cancellationToken)
            ?? throw new NotFoundException(nameof(Notification), request.NotificationId);

        // pre-check before the state moves (conflict -> 409): Queued is the
        // real move, Sent passes through so a redelivered confirm is a no-op
        // success - only states that can never become Sent refuse
        if (notification.Status is not (NotificationStatus.Queued or NotificationStatus.Sent))
            throw new ConflictException(
                $"Only queued notifications can be confirmed as sent (current: {notification.Status}).");

        notification.MarkSent();

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
