namespace CommerceFlow.Domain.Enums;

// How a notification reaches its recipient. InApp is delivered by the row
// itself (reading the feed IS the delivery, so no address is needed); the
// other channels hand the message to an external sender, which is exactly
// what the Queued state and the queue transport exist for.
public enum NotificationChannel
{
    InApp = 0,
    Email = 1,
    Sms = 2,
    Push = 3,
}
