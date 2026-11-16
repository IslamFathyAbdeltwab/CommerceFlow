using CommerceFlow.Domain.Common;
using CommerceFlow.Domain.Enums;
using CommerceFlow.Domain.Exceptions;

namespace CommerceFlow.Domain.Entities.Notifications;

// Aggregate root: one message, one recipient, one channel. "Queued" means
// handed to the transport - the domain never names a broker. RabbitMQ (or an
// in-process queue) is Infrastructure's concern: the Application layer pushes
// through a queue port when MarkQueued runs and confirms Sent or Failed when
// the consumer answers. Because that transport is at-least-once, every Mark*
// method treats a repeated confirmation as a no-op instead of an error - a
// redelivered message can never move a notification backwards or double-count
// a failure.
//
// Forward-only like the other modules, with one reversible move: a failed
// notification is re-queued for a retry (Failed -> Queued). The failure trail
// (reason, count, timestamp) stays for diagnostics until a later failure
// replaces it - it documents the previous attempt, not the current state.
public class Notification : AggregateRoot
{
    public const int SubjectMaxLength = 200;
    public const int BodyMaxLength = 4000;
    public const int AddressMaxLength = 320;
    public const int RelatedEntityTypeMaxLength = 100;
    public const int FailureReasonMaxLength = 500;

    // Forward-only transition table. Read is terminal (nothing happens to a
    // message the recipient has already seen); Failed leads back into the
    // pipeline only through an explicit retry.
    private static readonly IReadOnlyDictionary<NotificationStatus, NotificationStatus[]> AllowedTransitions =
        new Dictionary<NotificationStatus, NotificationStatus[]>
        {
            [NotificationStatus.Pending] = [NotificationStatus.Queued, NotificationStatus.Failed],
            [NotificationStatus.Queued]  = [NotificationStatus.Sent, NotificationStatus.Failed],
            [NotificationStatus.Sent]    = [NotificationStatus.Read, NotificationStatus.Failed],
            [NotificationStatus.Read]    = [],
            [NotificationStatus.Failed]  = [NotificationStatus.Queued],
        };

    public Guid RecipientId { get; private set; }
    public NotificationChannel Channel { get; private set; }
    public string Subject { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;

    // Resolved at creation so the sending side never reaches across modules
    // (email address, phone number, push token). In-app delivery needs none.
    public string? RecipientAddress { get; private set; }

    // Optional context link ("Order", orderId) so the recipient can be taken
    // to the thing this message is about.
    public string? RelatedEntityType { get; private set; }
    public Guid? RelatedEntityId { get; private set; }

    public NotificationStatus Status { get; private set; }

    public DateTimeOffset? QueuedAtUtc { get; private set; }
    public DateTimeOffset? SentAtUtc { get; private set; }
    public DateTimeOffset? ReadAtUtc { get; private set; }
    public DateTimeOffset? FailedAtUtc { get; private set; }

    public string? FailureReason { get; private set; }
    public int FailureCount { get; private set; }

    private Notification() { } // required by EF Core

    public static Notification Create(
        Guid recipientId,
        NotificationChannel channel,
        string subject,
        string body,
        string? recipientAddress = null,
        string? relatedEntityType = null,
        Guid? relatedEntityId = null)
    {
        if (recipientId == Guid.Empty)
            throw new DomainException("Recipient ID is required.");

        if (!Enum.IsDefined(channel))
            throw new DomainException("Notification channel is required.");

        var s = Required(subject, "Notification subject", SubjectMaxLength);
        var b = Required(body, "Notification body", BodyMaxLength);

        // In-app has no out-of-band target (the row is the delivery); every
        // real sender must know where to send before anything is queued
        string? address;
        if (channel == NotificationChannel.InApp)
        {
            address = Optional(recipientAddress, "Recipient address", AddressMaxLength);
        }
        else if (string.IsNullOrWhiteSpace(recipientAddress))
        {
            throw new DomainException(
                $"Recipient address is required for the {channel} channel.");
        }
        else
        {
            address = Required(recipientAddress, "Recipient address", AddressMaxLength);
        }

        var relatedType = Optional(relatedEntityType, "Related entity type", RelatedEntityTypeMaxLength);
        if (relatedEntityId is not null && relatedType is null)
            throw new DomainException("Related entity type is required when an id is provided.");
        if (relatedType is not null && relatedEntityId is null)
            throw new DomainException("Related entity id is required when a type is provided.");

        return new Notification
        {
            RecipientId = recipientId,
            Channel = channel,
            Subject = s,
            Body = b,
            RecipientAddress = address,
            RelatedEntityType = relatedType,
            RelatedEntityId = relatedEntityId,
            Status = NotificationStatus.Pending,
            FailureCount = 0,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        };
    }

    // Handed to the transport (queue publish). From Failed this is the retry:
    // the notification re-enters the pipeline while the previous failure's
    // trail stays recorded. A duplicate confirmation is a no-op - at-least-once
    // transports redeliver, and re-stamping would rewrite history.
    public void MarkQueued()
    {
        if (Status == NotificationStatus.Queued)
            return;

        MoveTo(NotificationStatus.Queued);
        QueuedAtUtc = DateTimeOffset.UtcNow; // last queue time: a retry re-stamps
    }

    // The consumer reports the channel accepted the message.
    public void MarkSent()
    {
        if (Status == NotificationStatus.Sent)
            return;

        MoveTo(NotificationStatus.Sent);
        SentAtUtc = DateTimeOffset.UtcNow;
    }

    // Recipient opened it. Read is terminal - later transport echoes change
    // nothing.
    public void MarkRead()
    {
        if (Status == NotificationStatus.Read)
            return;

        MoveTo(NotificationStatus.Read);
        ReadAtUtc = DateTimeOffset.UtcNow;
    }

    // The send failed (provider refused, bounced, timed out). A duplicate
    // failure report keeps the first reason and count; a later, genuinely
    // new failure after a retry replaces the reason and bumps the count.
    public void MarkFailed(string reason)
    {
        if (Status == NotificationStatus.Failed)
            return;

        // shape before transition: a failure report must say why, whatever
        // state it arrives against
        var r = Required(reason, "Failure reason", FailureReasonMaxLength);

        MoveTo(NotificationStatus.Failed);
        FailureReason = r;
        FailureCount++;
        FailedAtUtc = DateTimeOffset.UtcNow;
    }

    // ---------- Transition machine + validation ----------

    private void MoveTo(NotificationStatus target)
    {
        EnsureCanMoveTo(target);
        Status = target;
        Touch();
    }

    private void EnsureCanMoveTo(NotificationStatus target)
    {
        if (!AllowedTransitions[Status].Contains(target))
            throw new DomainException(
                $"Notification cannot move from {Status} to {target}.");
    }

    private void Touch() => UpdatedAtUtc = DateTimeOffset.UtcNow;

    private static string Required(string? value, string field, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException($"{field} is required.");

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
            throw new DomainException($"{field} cannot exceed {maxLength} characters.");

        return trimmed;
    }

    // Optional text: null/whitespace stays null, anything else is trimmed and
    // length-checked (same rule as Promotion's description).
    private static string? Optional(string? value, string field, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
            throw new DomainException($"{field} cannot exceed {maxLength} characters.");

        return trimmed;
    }
}
