using MediatR;

namespace CommerceFlow.Application.Notifications.Commands.FailNotification;

// Consumer (or sender) reports the delivery failed; the reason rides the
// body like RejectReview. A duplicate report of the same attempt is a no-op
// - the first reason and count stand - while a genuinely new failure after
// a retry replaces the reason and bumps the count (see Notification.Fail).
public sealed record FailNotificationCommand(
    Guid NotificationId,
    string Reason) : IRequest;
