using CommerceFlow.Domain.Entities.Notifications;
using CommerceFlow.Domain.Enums;
using CommerceFlow.Domain.Exceptions;

namespace CommerceFlow.Domain.Tests.Notifications;

public class NotificationTests
{
    private static readonly Guid RecipientId = Guid.NewGuid();
    private static readonly Guid RelatedId = Guid.NewGuid();

    private static Notification NewNotification(
        NotificationChannel channel = NotificationChannel.InApp,
        string subject = "  Your order has shipped  ",
        string body = "  Order 1042 is on its way.  ",
        string? recipientAddress = null,
        string? relatedEntityType = null,
        Guid? relatedEntityId = null) =>
        Notification.Create(
            RecipientId, channel, subject, body,
            recipientAddress, relatedEntityType, relatedEntityId);

    private static Notification QueuedNotification()
    {
        var notification = NewNotification();
        notification.MarkQueued();
        return notification;
    }

    private static Notification SentNotification()
    {
        var notification = QueuedNotification();
        notification.MarkSent();
        return notification;
    }

    // ---------- Create ----------

    [Fact]
    public void Create_ValidData_CreatesPendingNotification()
    {
        var notification = NewNotification();

        Assert.Equal(RecipientId, notification.RecipientId);
        Assert.Equal(NotificationChannel.InApp, notification.Channel);
        Assert.Equal("Your order has shipped", notification.Subject); // trimmed
        Assert.Equal("Order 1042 is on its way.", notification.Body); // trimmed
        Assert.Null(notification.RecipientAddress);
        Assert.Null(notification.RelatedEntityType);
        Assert.Null(notification.RelatedEntityId);
        Assert.Equal(NotificationStatus.Pending, notification.Status);
        Assert.Null(notification.QueuedAtUtc);
        Assert.Null(notification.SentAtUtc);
        Assert.Null(notification.ReadAtUtc);
        Assert.Null(notification.FailedAtUtc);
        Assert.Null(notification.FailureReason);
        Assert.Equal(0, notification.FailureCount);
        Assert.NotEqual(Guid.Empty, notification.Id);
    }

    [Fact]
    public void Create_SetsAuditTimestamps()
    {
        var notification = NewNotification();

        Assert.NotEqual(default, notification.CreatedAtUtc);
        Assert.NotEqual(default, notification.UpdatedAtUtc);
        Assert.True(notification.UpdatedAtUtc >= notification.CreatedAtUtc);
    }

    [Fact]
    public void Create_EmptyRecipientId_Throws()
    {
        var exception = Assert.Throws<DomainException>(() =>
            Notification.Create(Guid.Empty, NotificationChannel.InApp, "Subject", "Body"));

        Assert.Equal("Recipient ID is required.", exception.Message);
    }

    [Fact]
    public void Create_UnknownChannel_Throws()
    {
        var exception = Assert.Throws<DomainException>(() =>
            Notification.Create(RecipientId, (NotificationChannel)99, "Subject", "Body"));

        Assert.Equal("Notification channel is required.", exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptySubject_Throws(string subject)
    {
        var exception = Assert.Throws<DomainException>(() =>
            Notification.Create(RecipientId, NotificationChannel.InApp, subject, "Body"));

        Assert.Equal("Notification subject is required.", exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyBody_Throws(string body)
    {
        var exception = Assert.Throws<DomainException>(() =>
            Notification.Create(RecipientId, NotificationChannel.InApp, "Subject", body));

        Assert.Equal("Notification body is required.", exception.Message);
    }

    [Fact]
    public void Create_SubjectAtMaxLength_IsAllowed()
    {
        var notification = NewNotification(subject: new('s', Notification.SubjectMaxLength));

        Assert.Equal(Notification.SubjectMaxLength, notification.Subject.Length);
    }

    [Fact]
    public void Create_SubjectTooLong_Throws()
    {
        var exception = Assert.Throws<DomainException>(() =>
            NewNotification(subject: new('s', Notification.SubjectMaxLength + 1)));

        Assert.Equal(
            $"Notification subject cannot exceed {Notification.SubjectMaxLength} characters.",
            exception.Message);
    }

    [Fact]
    public void Create_BodyTooLong_Throws()
    {
        var exception = Assert.Throws<DomainException>(() =>
            NewNotification(body: new('b', Notification.BodyMaxLength + 1)));

        Assert.Equal(
            $"Notification body cannot exceed {Notification.BodyMaxLength} characters.",
            exception.Message);
    }

    [Theory]
    [InlineData(NotificationChannel.Email)]
    [InlineData(NotificationChannel.Sms)]
    [InlineData(NotificationChannel.Push)]
    public void Create_SenderChannelWithoutAddress_Throws(NotificationChannel channel)
    {
        // a real sender must know the target before anything is queued
        var exception = Assert.Throws<DomainException>(() =>
            NewNotification(channel: channel, recipientAddress: null));

        Assert.Equal(
            $"Recipient address is required for the {channel} channel.",
            exception.Message);
    }

    [Theory]
    [InlineData(NotificationChannel.Email)]
    [InlineData(NotificationChannel.Sms)]
    [InlineData(NotificationChannel.Push)]
    public void Create_SenderChannelWithWhitespaceAddress_Throws(NotificationChannel channel)
    {
        var exception = Assert.Throws<DomainException>(() =>
            NewNotification(channel: channel, recipientAddress: "   "));

        Assert.Equal(
            $"Recipient address is required for the {channel} channel.",
            exception.Message);
    }

    [Fact]
    public void Create_EmailWithAddress_TrimsIt()
    {
        var notification = NewNotification(
            channel: NotificationChannel.Email,
            recipientAddress: "  customer@example.com  ");

        Assert.Equal("customer@example.com", notification.RecipientAddress);
    }

    [Fact]
    public void Create_AddressTooLong_Throws()
    {
        var exception = Assert.Throws<DomainException>(() =>
            NewNotification(
                channel: NotificationChannel.Email,
                recipientAddress: new('a', Notification.AddressMaxLength + 1)));

        Assert.Equal(
            $"Recipient address cannot exceed {Notification.AddressMaxLength} characters.",
            exception.Message);
    }

    [Fact]
    public void Create_InAppWithWhitespaceAddress_BecomesNull()
    {
        // in-app has no out-of-band target: whitespace is "not applicable"
        var notification = NewNotification(recipientAddress: "   ");

        Assert.Null(notification.RecipientAddress);
    }

    [Fact]
    public void Create_InAppWithoutAddress_IsAllowed()
    {
        var notification = NewNotification();

        Assert.Null(notification.RecipientAddress);
        Assert.Equal(NotificationStatus.Pending, notification.Status);
    }

    [Fact]
    public void Create_RelatedIdWithoutType_Throws()
    {
        var exception = Assert.Throws<DomainException>(() =>
            NewNotification(relatedEntityId: RelatedId));

        Assert.Equal("Related entity type is required when an id is provided.", exception.Message);
    }

    [Fact]
    public void Create_RelatedTypeWithoutId_Throws()
    {
        var exception = Assert.Throws<DomainException>(() =>
            NewNotification(relatedEntityType: "Order"));

        Assert.Equal("Related entity id is required when a type is provided.", exception.Message);
    }

    [Fact]
    public void Create_WithRelatedEntity_KeepsBoth()
    {
        var notification = NewNotification(
            relatedEntityType: "  Order  ", relatedEntityId: RelatedId);

        Assert.Equal("Order", notification.RelatedEntityType); // trimmed
        Assert.Equal(RelatedId, notification.RelatedEntityId);
    }

    [Fact]
    public void Create_RelatedEntityTypeTooLong_Throws()
    {
        var exception = Assert.Throws<DomainException>(() =>
            NewNotification(
                relatedEntityType: new('t', Notification.RelatedEntityTypeMaxLength + 1),
                relatedEntityId: RelatedId));

        Assert.Equal(
            $"Related entity type cannot exceed {Notification.RelatedEntityTypeMaxLength} characters.",
            exception.Message);
    }

    // ---------- MarkQueued ----------

    [Fact]
    public void MarkQueued_FromPending_QueuesAndStamps()
    {
        var notification = NewNotification();

        notification.MarkQueued();

        Assert.Equal(NotificationStatus.Queued, notification.Status);
        Assert.NotNull(notification.QueuedAtUtc);
    }

    [Fact]
    public void MarkQueued_WhenAlreadyQueued_IsNoOp()
    {
        // at-least-once transports redeliver: a duplicate confirm must not
        // re-stamp or disturb the state
        var notification = QueuedNotification();
        var stamp = notification.QueuedAtUtc;

        notification.MarkQueued();

        Assert.Equal(NotificationStatus.Queued, notification.Status);
        Assert.Equal(stamp, notification.QueuedAtUtc);
    }

    [Fact]
    public void MarkQueued_FromFailed_RetriesKeepingFailureTrail()
    {
        var notification = QueuedNotification();
        notification.MarkFailed("SMTP connection timed out.");
        var failedAt = notification.FailedAtUtc;

        notification.MarkQueued(); // the one backwards move: retry

        Assert.Equal(NotificationStatus.Queued, notification.Status);
        Assert.NotNull(notification.QueuedAtUtc); // re-stamped for the new attempt
        Assert.Equal("SMTP connection timed out.", notification.FailureReason); // trail kept
        Assert.Equal(failedAt, notification.FailedAtUtc);
        Assert.Equal(1, notification.FailureCount);
    }

    [Fact]
    public void MarkQueued_FromSent_Throws()
    {
        var notification = SentNotification();

        var exception = Assert.Throws<DomainException>(() => notification.MarkQueued());

        Assert.Equal("Notification cannot move from Sent to Queued.", exception.Message);
        Assert.Equal(NotificationStatus.Sent, notification.Status); // nothing moved
    }

    [Fact]
    public void MarkQueued_FromRead_Throws()
    {
        var notification = SentNotification();
        notification.MarkRead();

        var exception = Assert.Throws<DomainException>(() => notification.MarkQueued());

        Assert.Equal("Notification cannot move from Read to Queued.", exception.Message);
    }

    // ---------- MarkSent ----------

    [Fact]
    public void MarkSent_FromQueued_SendsAndStamps()
    {
        var notification = QueuedNotification();

        notification.MarkSent();

        Assert.Equal(NotificationStatus.Sent, notification.Status);
        Assert.NotNull(notification.SentAtUtc);
    }

    [Fact]
    public void MarkSent_WhenAlreadySent_IsNoOp()
    {
        var notification = SentNotification();
        var stamp = notification.SentAtUtc;

        notification.MarkSent(); // duplicate delivery confirm

        Assert.Equal(NotificationStatus.Sent, notification.Status);
        Assert.Equal(stamp, notification.SentAtUtc);
    }

    [Theory]
    [InlineData(NotificationStatus.Pending)] // skipped the queue entirely
    [InlineData(NotificationStatus.Read)]
    [InlineData(NotificationStatus.Failed)]
    public void MarkSent_FromInvalidState_Throws(NotificationStatus status)
    {
        var notification = status switch
        {
            NotificationStatus.Pending => NewNotification(),
            NotificationStatus.Read => SentNotification().Also(n => n.MarkRead()),
            _ => QueuedNotification().Also(n => n.MarkFailed("Provider refused.")),
        };

        var exception = Assert.Throws<DomainException>(() => notification.MarkSent());

        Assert.Equal($"Notification cannot move from {status} to Sent.", exception.Message);
        Assert.Equal(status, notification.Status); // untouched
    }

    // ---------- MarkRead ----------

    [Fact]
    public void MarkRead_FromSent_ReadsAndStamps()
    {
        var notification = SentNotification();

        notification.MarkRead();

        Assert.Equal(NotificationStatus.Read, notification.Status);
        Assert.NotNull(notification.ReadAtUtc);
    }

    [Fact]
    public void MarkRead_WhenAlreadyRead_IsNoOp()
    {
        var notification = SentNotification();
        notification.MarkRead();
        var stamp = notification.ReadAtUtc;

        notification.MarkRead(); // later transport echo

        Assert.Equal(NotificationStatus.Read, notification.Status);
        Assert.Equal(stamp, notification.ReadAtUtc);
    }

    [Theory]
    [InlineData(NotificationStatus.Pending)]
    [InlineData(NotificationStatus.Queued)]
    [InlineData(NotificationStatus.Failed)]
    public void MarkRead_FromInvalidState_Throws(NotificationStatus status)
    {
        var notification = status switch
        {
            NotificationStatus.Pending => NewNotification(),
            NotificationStatus.Queued => QueuedNotification(),
            _ => QueuedNotification().Also(n => n.MarkFailed("Bounced.")),
        };

        var exception = Assert.Throws<DomainException>(() => notification.MarkRead());

        Assert.Equal($"Notification cannot move from {status} to Read.", exception.Message);
        Assert.Equal(status, notification.Status);
    }

    // ---------- MarkFailed ----------

    [Fact]
    public void MarkFailed_FromQueued_FailsWithTrimmedReasonAndCounts()
    {
        var notification = QueuedNotification();

        notification.MarkFailed("  SMTP connection timed out.  ");

        Assert.Equal(NotificationStatus.Failed, notification.Status);
        Assert.Equal("SMTP connection timed out.", notification.FailureReason); // trimmed
        Assert.Equal(1, notification.FailureCount);
        Assert.NotNull(notification.FailedAtUtc);
    }

    [Fact]
    public void MarkFailed_FromPending_FailsBeforeQueuing()
    {
        var notification = NewNotification();

        notification.MarkFailed("Recipient address invalid.");

        Assert.Equal(NotificationStatus.Failed, notification.Status);
        Assert.Equal(1, notification.FailureCount);
        Assert.Null(notification.QueuedAtUtc); // never made it to the queue
    }

    [Fact]
    public void MarkFailed_FromSent_FailsLateFailure()
    {
        var notification = SentNotification();

        notification.MarkFailed("Provider reported a bounce.");

        Assert.Equal(NotificationStatus.Failed, notification.Status);
        Assert.Equal(1, notification.FailureCount);
        Assert.NotNull(notification.SentAtUtc); // send attempt stays recorded
    }

    [Fact]
    public void MarkFailed_WhenAlreadyFailed_IsNoOpKeepsFirstReason()
    {
        // duplicate failure report of the same attempt (redelivered message)
        var notification = QueuedNotification();
        notification.MarkFailed("First reason.");
        var failedAt = notification.FailedAtUtc;

        notification.MarkFailed("Second reason.");

        Assert.Equal(NotificationStatus.Failed, notification.Status);
        Assert.Equal("First reason.", notification.FailureReason);
        Assert.Equal(1, notification.FailureCount); // not double-counted
        Assert.Equal(failedAt, notification.FailedAtUtc);
    }

    [Fact]
    public void MarkFailed_AfterRetry_CountsSecondFailureAndReplacesReason()
    {
        var notification = QueuedNotification();
        notification.MarkFailed("First reason.");
        notification.MarkQueued(); // retry

        notification.MarkFailed("Second reason."); // genuinely new failure

        Assert.Equal(NotificationStatus.Failed, notification.Status);
        Assert.Equal("Second reason.", notification.FailureReason);
        Assert.Equal(2, notification.FailureCount);
    }

    [Fact]
    public void MarkFailed_FromRead_Throws()
    {
        var notification = SentNotification();
        notification.MarkRead();

        var exception = Assert.Throws<DomainException>(() =>
            notification.MarkFailed("Too late."));

        Assert.Equal("Notification cannot move from Read to Failed.", exception.Message);
        Assert.Equal(NotificationStatus.Read, notification.Status);
        Assert.Equal(0, notification.FailureCount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void MarkFailed_EmptyReason_Throws(string reason)
    {
        // shape before transition: a failure report must say why
        var notification = QueuedNotification();

        var exception = Assert.Throws<DomainException>(() =>
            notification.MarkFailed(reason));

        Assert.Equal("Failure reason is required.", exception.Message);
        Assert.Equal(NotificationStatus.Queued, notification.Status); // untouched
    }

    [Fact]
    public void MarkFailed_ReasonTooLong_Throws()
    {
        var notification = QueuedNotification();

        var exception = Assert.Throws<DomainException>(() =>
            notification.MarkFailed(new('r', Notification.FailureReasonMaxLength + 1)));

        Assert.Equal(
            $"Failure reason cannot exceed {Notification.FailureReasonMaxLength} characters.",
            exception.Message);
        Assert.Equal(NotificationStatus.Queued, notification.Status);
    }
}

// Small helper so state setup reads as a single intention.
internal static class NotificationTestExtensions
{
    public static T Also<T>(this T value, Action<T> act)
    {
        act(value);
        return value;
    }
}
