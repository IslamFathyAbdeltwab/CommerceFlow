using CommerceFlow.Domain.Entities.Notifications;
using CommerceFlow.Domain.Enums;

namespace CommerceFlow.Application.Notifications.Queries;

// One DTO for detail and list reads (same call as PromotionDto): the whole
// delivery trail travels with the message so a feed row and an admin's
// failure view need no second round-trip.
public sealed record NotificationDto(
    Guid Id,
    Guid RecipientId,
    NotificationChannel Channel,
    string Subject,
    string Body,
    string? RecipientAddress,
    string? RelatedEntityType,
    Guid? RelatedEntityId,
    NotificationStatus Status,
    DateTimeOffset? QueuedAtUtc,
    DateTimeOffset? SentAtUtc,
    DateTimeOffset? ReadAtUtc,
    DateTimeOffset? FailedAtUtc,
    string? FailureReason,
    int FailureCount,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

// Centralizes entity -> DTO mapping so the handlers stay thin.
internal static class NotificationMappings
{
    public static NotificationDto ToDto(Notification notification) => new(
        notification.Id,
        notification.RecipientId,
        notification.Channel,
        notification.Subject,
        notification.Body,
        notification.RecipientAddress,
        notification.RelatedEntityType,
        notification.RelatedEntityId,
        notification.Status,
        notification.QueuedAtUtc,
        notification.SentAtUtc,
        notification.ReadAtUtc,
        notification.FailedAtUtc,
        notification.FailureReason,
        notification.FailureCount,
        notification.CreatedAtUtc,
        notification.UpdatedAtUtc);
}
