using MediatR;

namespace CommerceFlow.Application.Notifications.Commands.MarkNotificationSent;

// Consumer confirmation: the channel accepted the message. The transport is
// at-least-once, so confirming an already-Sent notification is a success
// (204), not a conflict - duplicate delivery reports are normal traffic.
public sealed record MarkNotificationSentCommand(Guid NotificationId) : IRequest;
