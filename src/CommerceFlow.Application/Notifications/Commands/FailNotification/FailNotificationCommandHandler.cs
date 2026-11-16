using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Notifications.Interfaces;
using CommerceFlow.Domain.Entities.Notifications;
using CommerceFlow.Domain.Enums;
using MediatR;

namespace CommerceFlow.Application.Notifications.Commands.FailNotification;

public sealed class FailNotificationCommandHandler(
    INotificationRepository notifications,
    IUnitOfWork unitOfWork) : IRequestHandler<FailNotificationCommand>
{
    public async Task Handle(FailNotificationCommand request, CancellationToken cancellationToken)
    {
        var notification = await notifications.GetByIdAsync(request.NotificationId, cancellationToken)
            ?? throw new NotFoundException(nameof(Notification), request.NotificationId);

        // pre-check before the state moves (conflict -> 409): everything not
        // yet seen by the recipient may fail - including an already-Failed
        // one, whose duplicate report the domain swallows (first reason
        // stands). Only Read, the delivered-and-seen state, refuses.
        if (notification.Status == NotificationStatus.Read)
            throw new ConflictException(
                $"Only undelivered notifications can be marked as failed (current: {notification.Status}).");

        // empty/too-long reason is a shape problem the domain still guards
        // (DomainException -> 400) as its safety net behind the validator
        notification.MarkFailed(request.Reason);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
