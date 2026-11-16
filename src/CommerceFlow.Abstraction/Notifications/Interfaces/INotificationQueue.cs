using CommerceFlow.Domain.Entities.Notifications;

namespace CommerceFlow.Application.Notifications.Interfaces;

// Transport port: how a queued notification leaves the process. RabbitMQ is
// one implementation (Infrastructure), an in-process channel another - the
// Application layer only ever sees this port, which is what keeps the broker
// out of the Domain entirely. Delivery is at-least-once: the consumer's
// confirmations run through the aggregate's idempotent Mark* methods, so a
// duplicate publish or a redelivered message can never double-send,
// double-count a failure or move a notification backwards.
public interface INotificationQueue
{
    Task PublishAsync(Notification notification, CancellationToken cancellationToken = default);
}
