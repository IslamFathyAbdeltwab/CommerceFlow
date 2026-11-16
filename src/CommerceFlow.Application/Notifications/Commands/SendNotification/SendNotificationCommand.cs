using CommerceFlow.Domain.Enums;
using MediatR;

namespace CommerceFlow.Application.Notifications.Commands.SendNotification;

// Creates a notification for one recipient on one channel, hands it to the
// transport and persists it. The channel/address and related-entity pairing
// rules are the domain's (DomainException -> 400); there is no uniqueness
// pre-check because a notification is addressed to a person, not to a key.
// Returns the notification id.
public sealed record SendNotificationCommand(
    Guid RecipientId,
    NotificationChannel Channel,
    string Subject,
    string Body,
    string? RecipientAddress = null,
    string? RelatedEntityType = null,
    Guid? RelatedEntityId = null) : IRequest<Guid>;
