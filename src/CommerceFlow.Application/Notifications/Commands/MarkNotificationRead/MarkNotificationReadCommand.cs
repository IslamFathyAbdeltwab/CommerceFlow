using MediatR;

namespace CommerceFlow.Application.Notifications.Commands.MarkNotificationRead;

// Recipient opened the message (the in-app feed's own read receipt). Like
// the other confirms, marking an already-Read notification is a no-op
// success - opening the feed twice must not restamp or conflict.
public sealed record MarkNotificationReadCommand(Guid NotificationId) : IRequest;
