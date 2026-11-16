using CommerceFlow.Application.Notifications.Commands.SendNotification;
using CommerceFlow.Domain.Entities.Notifications;
using CommerceFlow.Domain.Enums;

namespace CommerceFlow.Application.Tests.Notifications;

// Shared factories for building notifications (and the send command). The
// status switch walks the real state machine - a fixture can only reach a
// state the domain itself allows, so the tests never fake a path that does
// not exist. In-app needs no address; sender channels get one by default.
internal static class NotificationsFixtures
{
    public static readonly Guid RecipientId = Guid.Parse("77777777-7777-7777-7777-777777777777");

    public static Notification CreateNotification(
        Guid? recipientId = null,
        NotificationChannel channel = NotificationChannel.InApp,
        string subject = "Order shipped",
        string body = "Your order is on its way.",
        string? recipientAddress = null,
        string? relatedEntityType = null,
        Guid? relatedEntityId = null,
        NotificationStatus status = NotificationStatus.Pending,
        string failureReason = "Provider refused the message.")
    {
        var notification = Notification.Create(
            recipientId ?? RecipientId,
            channel,
            subject,
            body,
            recipientAddress
                ?? (channel == NotificationChannel.InApp ? null : "user@example.com"),
            relatedEntityType,
            relatedEntityId);

        switch (status)
        {
            case NotificationStatus.Pending:
                break;
            case NotificationStatus.Queued:
                notification.MarkQueued();
                break;
            case NotificationStatus.Sent:
                notification.MarkQueued();
                notification.MarkSent();
                break;
            case NotificationStatus.Read:
                notification.MarkQueued();
                notification.MarkSent();
                notification.MarkRead();
                break;
            case NotificationStatus.Failed:
                notification.MarkQueued();
                notification.MarkFailed(failureReason);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(status), status, null);
        }

        return notification;
    }

    public static SendNotificationCommand ValidSendCommand(
        Guid? recipientId = null,
        NotificationChannel channel = NotificationChannel.InApp,
        string subject = "Order shipped",
        string body = "Your order is on its way.",
        string? recipientAddress = null,
        string? relatedEntityType = null,
        Guid? relatedEntityId = null) =>
        new(
            recipientId ?? RecipientId,
            channel,
            subject,
            body,
            recipientAddress,
            relatedEntityType,
            relatedEntityId);
}
