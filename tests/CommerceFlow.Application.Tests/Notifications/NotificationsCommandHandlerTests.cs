using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Notifications.Commands.FailNotification;
using CommerceFlow.Application.Notifications.Commands.MarkNotificationRead;
using CommerceFlow.Application.Notifications.Commands.MarkNotificationSent;
using CommerceFlow.Application.Notifications.Commands.RetryNotification;
using CommerceFlow.Application.Notifications.Commands.SendNotification;
using CommerceFlow.Application.Notifications.Interfaces;
using CommerceFlow.Domain.Entities.Notifications;
using CommerceFlow.Domain.Enums;
using CommerceFlow.Domain.Exceptions;
using Moq;

namespace CommerceFlow.Application.Tests.Notifications;

public sealed class NotificationsCommandHandlerTests
{
    private readonly Mock<INotificationRepository> _notifications = new();
    private readonly Mock<INotificationQueue> _queue = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private void SetupNotification(Notification notification) =>
        _notifications
            .Setup(x => x.GetByIdAsync(notification.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(notification);

    private void SetupNotificationMissing(Guid id) =>
        _notifications
            .Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Notification?)null);

    private static void VerifyNeverSaved(Mock<IUnitOfWork> unitOfWork) =>
        unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

    private static void VerifyNeverPublished(Mock<INotificationQueue> queue) =>
        queue.Verify(
            x => x.PublishAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()),
            Times.Never);

    // Records the handler's call order so the publish-before-commit design is
    // asserted, not just commented: transport first, row second, commit last.
    private List<string> TrackCallOrder()
    {
        var order = new List<string>();

        _queue
            .Setup(x => x.PublishAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
            .Callback((Notification _, CancellationToken _) => order.Add("publish"));
        _notifications
            .Setup(x => x.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
            .Callback((Notification _, CancellationToken _) => order.Add("add"));
        _unitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback((CancellationToken _) => order.Add("save"));

        return order;
    }

    // ---------- SendNotification ----------

    [Fact]
    public async Task Send_WithValidInput_PublishesQueuedRowAndSaves()
    {
        var handler = new SendNotificationCommandHandler(
            _notifications.Object, _queue.Object, _unitOfWork.Object);
        var order = TrackCallOrder();

        var id = await handler.Handle(
            NotificationsFixtures.ValidSendCommand(
                subject: "  Order shipped  ", body: "  Your order is on its way.  "),
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, id);
        _notifications.Verify(
            x => x.AddAsync(
                It.Is<Notification>(n => n.Id == id
                    && n.RecipientId == NotificationsFixtures.RecipientId
                    && n.Channel == NotificationChannel.InApp
                    && n.Subject == "Order shipped" // trimmed by the domain
                    && n.Body == "Your order is on its way." // trimmed by the domain
                    && n.Status == NotificationStatus.Queued // handed to the transport
                    && n.QueuedAtUtc != null
                    && n.FailureCount == 0),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _queue.Verify(
            x => x.PublishAsync(
                It.Is<Notification>(n => n.Id == id
                    && n.Status == NotificationStatus.Queued), // published after MarkQueued
                It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        // transport first, row second, commit last
        Assert.Equal(new[] { "publish", "add", "save" }, order);
    }

    [Fact]
    public async Task Send_WhenTransportRefuses_NeverAddsOrSaves()
    {
        var handler = new SendNotificationCommandHandler(
            _notifications.Object, _queue.Object, _unitOfWork.Object);
        _queue
            .Setup(x => x.PublishAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("broker unreachable"));

        // publish-before-commit in reverse: a refused publish must leave no
        // row behind, or the database would hold messages the transport never
        // took
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(NotificationsFixtures.ValidSendCommand(), CancellationToken.None));

        _notifications.Verify(
            x => x.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()),
            Times.Never);
        VerifyNeverSaved(_unitOfWork);
    }

    [Fact]
    public async Task Send_OnSenderChannelWithoutAddress_ThrowsDomainException()
    {
        var handler = new SendNotificationCommandHandler(
            _notifications.Object, _queue.Object, _unitOfWork.Object);

        // the validator lets this through (shape only); the domain refuses
        // because a sender with no target cannot deliver
        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(
                NotificationsFixtures.ValidSendCommand(
                    channel: NotificationChannel.Email, recipientAddress: null),
                CancellationToken.None));

        Assert.Contains(
            "Recipient address is required for the Email channel.", exception.Message);
        VerifyNeverPublished(_queue);
        _notifications.Verify(
            x => x.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()),
            Times.Never);
        VerifyNeverSaved(_unitOfWork);
    }

    [Fact]
    public async Task Send_WithRelatedEntityIdOnly_ThrowsDomainException()
    {
        var handler = new SendNotificationCommandHandler(
            _notifications.Object, _queue.Object, _unitOfWork.Object);

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(
                NotificationsFixtures.ValidSendCommand(relatedEntityId: Guid.NewGuid()),
                CancellationToken.None));

        Assert.Contains(
            "Related entity type is required when an id is provided.", exception.Message);
        VerifyNeverPublished(_queue);
        VerifyNeverSaved(_unitOfWork);
    }

    [Fact]
    public async Task Send_WithRelatedEntityTypeOnly_ThrowsDomainException()
    {
        var handler = new SendNotificationCommandHandler(
            _notifications.Object, _queue.Object, _unitOfWork.Object);

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(
                NotificationsFixtures.ValidSendCommand(relatedEntityType: "Order"),
                CancellationToken.None));

        Assert.Contains(
            "Related entity id is required when a type is provided.", exception.Message);
        VerifyNeverPublished(_queue);
        VerifyNeverSaved(_unitOfWork);
    }

    // ---------- MarkNotificationSent ----------

    [Fact]
    public async Task MarkSent_FromQueued_ConfirmsAndSaves()
    {
        var handler = new MarkNotificationSentCommandHandler(
            _notifications.Object, _unitOfWork.Object);
        var notification = NotificationsFixtures.CreateNotification(
            status: NotificationStatus.Queued);
        SetupNotification(notification);

        await handler.Handle(
            new MarkNotificationSentCommand(notification.Id), CancellationToken.None);

        Assert.Equal(NotificationStatus.Sent, notification.Status);
        Assert.NotNull(notification.SentAtUtc);
        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        // a confirm never re-publishes: the transport already delivered this
        VerifyNeverPublished(_queue);
    }

    [Fact]
    public async Task MarkSent_OnDuplicateConfirm_IsIdempotentNoOp()
    {
        var handler = new MarkNotificationSentCommandHandler(
            _notifications.Object, _unitOfWork.Object);
        var notification = NotificationsFixtures.CreateNotification(
            status: NotificationStatus.Sent);
        var originalStamp = notification.SentAtUtc;
        SetupNotification(notification);

        // at-least-once transports redeliver confirms: the second report is a
        // success (204), not a conflict, and must not restamp history
        await handler.Handle(
            new MarkNotificationSentCommand(notification.Id), CancellationToken.None);

        Assert.Equal(NotificationStatus.Sent, notification.Status);
        Assert.Equal(originalStamp, notification.SentAtUtc);
        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(NotificationStatus.Pending)]
    [InlineData(NotificationStatus.Read)]
    [InlineData(NotificationStatus.Failed)]
    public async Task MarkSent_FromStateThatCannotBeSent_ThrowsConflict(NotificationStatus status)
    {
        var handler = new MarkNotificationSentCommandHandler(
            _notifications.Object, _unitOfWork.Object);
        var notification = NotificationsFixtures.CreateNotification(status: status);
        SetupNotification(notification);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(
                new MarkNotificationSentCommand(notification.Id), CancellationToken.None));

        Assert.Contains(
            $"Only queued notifications can be confirmed as sent (current: {status}).",
            exception.Message);
        Assert.Equal(status, notification.Status); // untouched
        VerifyNeverSaved(_unitOfWork);
    }

    [Fact]
    public async Task MarkSent_WhenNotificationDoesNotExist_ThrowsNotFound()
    {
        var handler = new MarkNotificationSentCommandHandler(
            _notifications.Object, _unitOfWork.Object);
        SetupNotificationMissing(Guid.NewGuid());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new MarkNotificationSentCommand(Guid.NewGuid()), CancellationToken.None));

        VerifyNeverSaved(_unitOfWork);
    }

    // ---------- MarkNotificationRead ----------

    [Fact]
    public async Task MarkRead_FromSent_MarksReadAndSaves()
    {
        var handler = new MarkNotificationReadCommandHandler(
            _notifications.Object, _unitOfWork.Object);
        var notification = NotificationsFixtures.CreateNotification(
            status: NotificationStatus.Sent);
        SetupNotification(notification);

        await handler.Handle(
            new MarkNotificationReadCommand(notification.Id), CancellationToken.None);

        Assert.Equal(NotificationStatus.Read, notification.Status);
        Assert.NotNull(notification.ReadAtUtc);
        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MarkRead_OnDuplicateOpen_IsIdempotentNoOp()
    {
        var handler = new MarkNotificationReadCommandHandler(
            _notifications.Object, _unitOfWork.Object);
        var notification = NotificationsFixtures.CreateNotification(
            status: NotificationStatus.Read);
        var originalStamp = notification.ReadAtUtc;
        SetupNotification(notification);

        // opening the feed twice must not restamp or conflict
        await handler.Handle(
            new MarkNotificationReadCommand(notification.Id), CancellationToken.None);

        Assert.Equal(NotificationStatus.Read, notification.Status);
        Assert.Equal(originalStamp, notification.ReadAtUtc);
        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(NotificationStatus.Pending)]
    [InlineData(NotificationStatus.Queued)]
    [InlineData(NotificationStatus.Failed)]
    public async Task MarkRead_WhenNotYetDelivered_ThrowsConflict(NotificationStatus status)
    {
        var handler = new MarkNotificationReadCommandHandler(
            _notifications.Object, _unitOfWork.Object);
        var notification = NotificationsFixtures.CreateNotification(status: status);
        SetupNotification(notification);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(
                new MarkNotificationReadCommand(notification.Id), CancellationToken.None));

        Assert.Contains(
            $"Only sent notifications can be marked as read (current: {status}).",
            exception.Message);
        Assert.Equal(status, notification.Status); // untouched
        VerifyNeverSaved(_unitOfWork);
    }

    [Fact]
    public async Task MarkRead_WhenNotificationDoesNotExist_ThrowsNotFound()
    {
        var handler = new MarkNotificationReadCommandHandler(
            _notifications.Object, _unitOfWork.Object);
        SetupNotificationMissing(Guid.NewGuid());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new MarkNotificationReadCommand(Guid.NewGuid()), CancellationToken.None));

        VerifyNeverSaved(_unitOfWork);
    }

    // ---------- FailNotification ----------

    [Fact]
    public async Task Fail_FromQueued_RecordsReasonAndSaves()
    {
        var handler = new FailNotificationCommandHandler(
            _notifications.Object, _unitOfWork.Object);
        var notification = NotificationsFixtures.CreateNotification(
            status: NotificationStatus.Queued);
        SetupNotification(notification);

        await handler.Handle(
            new FailNotificationCommand(notification.Id, "  SMTP timeout  "),
            CancellationToken.None);

        Assert.Equal(NotificationStatus.Failed, notification.Status);
        Assert.Equal("SMTP timeout", notification.FailureReason); // trimmed
        Assert.Equal(1, notification.FailureCount);
        Assert.NotNull(notification.FailedAtUtc);
        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Fail_FromPending_RecordsReasonAndSaves()
    {
        var handler = new FailNotificationCommandHandler(
            _notifications.Object, _unitOfWork.Object);
        var notification = NotificationsFixtures.CreateNotification(
            status: NotificationStatus.Pending);
        SetupNotification(notification);

        // a sender may refuse before the transport ever saw the message -
        // everything not yet seen by the recipient can fail
        await handler.Handle(
            new FailNotificationCommand(notification.Id, "Invalid address"),
            CancellationToken.None);

        Assert.Equal(NotificationStatus.Failed, notification.Status);
        Assert.Equal("Invalid address", notification.FailureReason);
        Assert.Equal(1, notification.FailureCount);
        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Fail_OnDuplicateReport_KeepsFirstReasonAndCount()
    {
        var handler = new FailNotificationCommandHandler(
            _notifications.Object, _unitOfWork.Object);
        var notification = NotificationsFixtures.CreateNotification(
            status: NotificationStatus.Failed, failureReason: "First attempt died");
        var originalStamp = notification.FailedAtUtc;
        SetupNotification(notification);

        // the consumer reports the same failure twice: the first reason and
        // count stand - a duplicate is not a new failure
        await handler.Handle(
            new FailNotificationCommand(notification.Id, "Second report of the same"),
            CancellationToken.None);

        Assert.Equal(NotificationStatus.Failed, notification.Status);
        Assert.Equal("First attempt died", notification.FailureReason);
        Assert.Equal(1, notification.FailureCount);
        Assert.Equal(originalStamp, notification.FailedAtUtc);
        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Fail_FromRead_ThrowsConflict()
    {
        var handler = new FailNotificationCommandHandler(
            _notifications.Object, _unitOfWork.Object);
        var notification = NotificationsFixtures.CreateNotification(
            status: NotificationStatus.Read);
        var originalStamp = notification.ReadAtUtc;
        SetupNotification(notification);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(
                new FailNotificationCommand(notification.Id, "Too late to fail"),
                CancellationToken.None));

        Assert.Contains(
            "Only undelivered notifications can be marked as failed (current: Read).",
            exception.Message);
        Assert.Equal(NotificationStatus.Read, notification.Status); // delivered-and-seen
        Assert.Equal(originalStamp, notification.ReadAtUtc);
        VerifyNeverSaved(_unitOfWork);
    }

    [Fact]
    public async Task Fail_WithEmptyReason_ThrowsDomainException()
    {
        var handler = new FailNotificationCommandHandler(
            _notifications.Object, _unitOfWork.Object);
        var notification = NotificationsFixtures.CreateNotification(
            status: NotificationStatus.Queued);
        SetupNotification(notification);

        // shape-before-transition safety net: the validator normally stops
        // this, the domain refuses it again if a call skips validation
        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(
                new FailNotificationCommand(notification.Id, "   "),
                CancellationToken.None));

        Assert.Contains("Failure reason is required.", exception.Message);
        Assert.Equal(NotificationStatus.Queued, notification.Status); // never moved
        VerifyNeverSaved(_unitOfWork);
    }

    [Fact]
    public async Task Fail_WhenNotificationDoesNotExist_ThrowsNotFound()
    {
        var handler = new FailNotificationCommandHandler(
            _notifications.Object, _unitOfWork.Object);
        SetupNotificationMissing(Guid.NewGuid());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new FailNotificationCommand(Guid.NewGuid(), "reason"),
                CancellationToken.None));

        VerifyNeverSaved(_unitOfWork);
    }

    // ---------- RetryNotification ----------

    [Fact]
    public async Task Retry_FromFailed_RequeuesRepublishesAndSaves()
    {
        var handler = new RetryNotificationCommandHandler(
            _notifications.Object, _queue.Object, _unitOfWork.Object);
        var notification = NotificationsFixtures.CreateNotification(
            status: NotificationStatus.Failed, failureReason: "Provider refused");
        var originalFailedAt = notification.FailedAtUtc;
        SetupNotification(notification);
        var order = new List<string>();
        _queue
            .Setup(x => x.PublishAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
            .Callback((Notification _, CancellationToken _) => order.Add("publish"));
        _unitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback((CancellationToken _) => order.Add("save"));

        await handler.Handle(
            new RetryNotificationCommand(notification.Id), CancellationToken.None);

        Assert.Equal(NotificationStatus.Queued, notification.Status); // the one backwards move
        Assert.NotNull(notification.QueuedAtUtc);
        // the failure trail survives the retry: it documents the previous
        // attempt, it is not the current state
        Assert.Equal("Provider refused", notification.FailureReason);
        Assert.Equal(1, notification.FailureCount);
        Assert.Equal(originalFailedAt, notification.FailedAtUtc);
        _queue.Verify(
            x => x.PublishAsync(
                It.Is<Notification>(n => n.Id == notification.Id
                    && n.Status == NotificationStatus.Queued),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(new[] { "publish", "save" }, order); // back into the transport before commit
    }

    [Theory]
    [InlineData(NotificationStatus.Pending)]
    [InlineData(NotificationStatus.Queued)]
    [InlineData(NotificationStatus.Sent)]
    [InlineData(NotificationStatus.Read)]
    public async Task Retry_WhenNotFailed_ThrowsConflict(NotificationStatus status)
    {
        var handler = new RetryNotificationCommandHandler(
            _notifications.Object, _queue.Object, _unitOfWork.Object);
        var notification = NotificationsFixtures.CreateNotification(status: status);
        var originalQueuedAt = notification.QueuedAtUtc;
        SetupNotification(notification);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(
                new RetryNotificationCommand(notification.Id), CancellationToken.None));

        Assert.Contains(
            $"Only failed notifications can be retried (current: {status}).",
            exception.Message);
        Assert.Equal(status, notification.Status); // untouched
        Assert.Equal(originalQueuedAt, notification.QueuedAtUtc);
        VerifyNeverPublished(_queue);
        VerifyNeverSaved(_unitOfWork);
    }

    [Fact]
    public async Task Retry_WhenTransportRefuses_NeverSaves()
    {
        var handler = new RetryNotificationCommandHandler(
            _notifications.Object, _queue.Object, _unitOfWork.Object);
        var notification = NotificationsFixtures.CreateNotification(
            status: NotificationStatus.Failed);
        SetupNotification(notification);
        _queue
            .Setup(x => x.PublishAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("broker unreachable"));

        // same publish-before-commit rule as Send: a refused publish means
        // nothing is committed (the row stays Failed on disk, retryable again)
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new RetryNotificationCommand(notification.Id), CancellationToken.None));

        VerifyNeverSaved(_unitOfWork);
    }

    [Fact]
    public async Task Retry_WhenNotificationDoesNotExist_ThrowsNotFound()
    {
        var handler = new RetryNotificationCommandHandler(
            _notifications.Object, _queue.Object, _unitOfWork.Object);
        SetupNotificationMissing(Guid.NewGuid());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new RetryNotificationCommand(Guid.NewGuid()), CancellationToken.None));

        VerifyNeverPublished(_queue);
        VerifyNeverSaved(_unitOfWork);
    }
}
