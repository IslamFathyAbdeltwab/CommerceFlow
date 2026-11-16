using MediatR;

namespace CommerceFlow.Application.Notifications.Commands.RetryNotification;

// Operator re-queues a failed notification: Failed -> Queued (the one
// backwards move) and a fresh publish. The failure trail stays recorded, so
// the audit of why the previous attempt died survives the retry.
public sealed record RetryNotificationCommand(Guid NotificationId) : IRequest;
