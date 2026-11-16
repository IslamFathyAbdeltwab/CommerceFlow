using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Notifications.Interfaces;
using CommerceFlow.Domain.Entities.Notifications;
using MediatR;

namespace CommerceFlow.Application.Notifications.Commands.SendNotification;

public sealed class SendNotificationCommandHandler(
    INotificationRepository notifications,
    INotificationQueue queue,
    IUnitOfWork unitOfWork) : IRequestHandler<SendNotificationCommand, Guid>
{
    public async Task<Guid> Handle(SendNotificationCommand request, CancellationToken cancellationToken)
    {
        var notification = Notification.Create(
            request.RecipientId,
            request.Channel,
            request.Subject,
            request.Body,
            request.RecipientAddress,
            request.RelatedEntityType,
            request.RelatedEntityId);

        notification.MarkQueued();

        // Publish before commit: the transport accepts the message first and
        // the row lands after. A consumer racing the commit finds no row,
        // NACKs and retries (at-least-once); the reverse order could strand a
        // Queued row no message will ever arrive for. An outbox would make
        // the pair atomic - the idempotent Mark* confirms keep this window
        // safe until then.
        await queue.PublishAsync(notification, cancellationToken);

        await notifications.AddAsync(notification, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return notification.Id;
    }
}
