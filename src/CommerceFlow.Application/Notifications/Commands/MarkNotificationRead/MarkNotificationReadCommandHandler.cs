using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Notifications.Interfaces;
using CommerceFlow.Domain.Entities.Notifications;
using CommerceFlow.Domain.Enums;
using MediatR;

namespace CommerceFlow.Application.Notifications.Commands.MarkNotificationRead;

public sealed class MarkNotificationReadCommandHandler(
    INotificationRepository notifications,
    IUnitOfWork unitOfWork) : IRequestHandler<MarkNotificationReadCommand>
{
    public async Task Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
    {
        var notification = await notifications.GetByIdAsync(request.NotificationId, cancellationToken)
            ?? throw new NotFoundException(nameof(Notification), request.NotificationId);

        // pre-check before the state moves (conflict -> 409): Sent is the
        // real move, Read passes through (duplicate open = no-op success) -
        // everything else has not reached the recipient yet
        if (notification.Status is not (NotificationStatus.Sent or NotificationStatus.Read))
            throw new ConflictException(
                $"Only sent notifications can be marked as read (current: {notification.Status}).");

        notification.MarkRead();

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
