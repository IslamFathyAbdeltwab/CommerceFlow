using CommerceFlow.Application.Notifications.Interfaces;
using CommerceFlow.Application.Notifications.Queries;
using CommerceFlow.Application.Notifications.Queries.GetNotificationById;
using CommerceFlow.Application.Notifications.Queries.GetNotifications;
using CommerceFlow.Domain.Entities.Notifications;
using CommerceFlow.Domain.Enums;
using Moq;

namespace CommerceFlow.Application.Tests.Notifications;

public sealed class NotificationsQueryHandlerTests
{
    private readonly Mock<INotificationRepository> _notifications = new();

    private void SetupNotification(Notification notification) =>
        _notifications
            .Setup(x => x.GetByIdAsync(notification.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(notification);

    // ---------- GetNotificationById ----------

    [Fact]
    public async Task GetNotificationById_WhenNotificationDoesNotExist_ReturnsNull()
    {
        var handler = new GetNotificationByIdQueryHandler(_notifications.Object);

        var result = await handler.Handle(
            new GetNotificationByIdQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Null(result); // controller turns this into 404
    }

    [Fact]
    public async Task GetNotificationById_WhenQueued_MapsAllFields()
    {
        var handler = new GetNotificationByIdQueryHandler(_notifications.Object);
        var relatedId = Guid.NewGuid();
        var notification = NotificationsFixtures.CreateNotification(
            channel: NotificationChannel.Email,
            recipientAddress: "user@example.com",
            relatedEntityType: "Order",
            relatedEntityId: relatedId,
            status: NotificationStatus.Queued);
        SetupNotification(notification);

        var dto = await handler.Handle(
            new GetNotificationByIdQuery(notification.Id), CancellationToken.None);

        Assert.NotNull(dto);
        Assert.Equal(notification.Id, dto.Id);
        Assert.Equal(NotificationsFixtures.RecipientId, dto.RecipientId);
        Assert.Equal(NotificationChannel.Email, dto.Channel);
        Assert.Equal("Order shipped", dto.Subject);
        Assert.Equal("Your order is on its way.", dto.Body);
        Assert.Equal("user@example.com", dto.RecipientAddress);
        Assert.Equal("Order", dto.RelatedEntityType);
        Assert.Equal(relatedId, dto.RelatedEntityId);
        Assert.Equal(NotificationStatus.Queued, dto.Status);
        Assert.NotNull(dto.QueuedAtUtc);
        Assert.Null(dto.SentAtUtc);
        Assert.Null(dto.ReadAtUtc);
        Assert.Null(dto.FailedAtUtc);
        Assert.Null(dto.FailureReason);
        Assert.Equal(0, dto.FailureCount);
        Assert.Equal(notification.CreatedAtUtc, dto.CreatedAtUtc);
        Assert.Equal(notification.UpdatedAtUtc, dto.UpdatedAtUtc);
    }

    [Fact]
    public async Task GetNotificationById_WhenFailed_MapsFailureTrail()
    {
        var handler = new GetNotificationByIdQueryHandler(_notifications.Object);
        var notification = NotificationsFixtures.CreateNotification(
            status: NotificationStatus.Failed, failureReason: "Provider refused");
        SetupNotification(notification);

        var dto = await handler.Handle(
            new GetNotificationByIdQuery(notification.Id), CancellationToken.None);

        Assert.NotNull(dto);
        Assert.Equal(NotificationStatus.Failed, dto.Status);
        Assert.Equal("Provider refused", dto.FailureReason);
        Assert.Equal(1, dto.FailureCount);
        Assert.NotNull(dto.FailedAtUtc);
        Assert.NotNull(dto.QueuedAtUtc); // it did reach the transport first
        Assert.Null(dto.SentAtUtc); // but never delivered
        Assert.Null(dto.ReadAtUtc);
    }

    [Fact]
    public async Task GetNotificationById_WhenInApp_MapsNullAddress()
    {
        var handler = new GetNotificationByIdQueryHandler(_notifications.Object);
        var notification = NotificationsFixtures.CreateNotification(
            channel: NotificationChannel.InApp); // no out-of-band target
        SetupNotification(notification);

        var dto = await handler.Handle(
            new GetNotificationByIdQuery(notification.Id), CancellationToken.None);

        Assert.NotNull(dto);
        Assert.Equal(NotificationChannel.InApp, dto.Channel);
        Assert.Null(dto.RecipientAddress);
        Assert.Equal(NotificationStatus.Pending, dto.Status);
        Assert.Null(dto.QueuedAtUtc);
    }

    // ---------- GetNotifications ----------

    [Fact]
    public async Task GetNotifications_WhenNoneExist_ReturnsEmptyPagedResult()
    {
        var handler = new GetNotificationsQueryHandler(_notifications.Object);
        _notifications
            .Setup(x => x.CountAsync(
                It.IsAny<Guid?>(), It.IsAny<NotificationStatus?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _notifications
            .Setup(x => x.GetAllAsync(
                It.IsAny<Guid?>(), It.IsAny<NotificationStatus?>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Notification>());

        var result = await handler.Handle(new GetNotificationsQuery(), CancellationToken.None);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(20, result.PageSize);
        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    public async Task GetNotifications_WithFilters_PassesThemThroughAndMapsItems()
    {
        var handler = new GetNotificationsQueryHandler(_notifications.Object);
        var first = NotificationsFixtures.CreateNotification(
            subject: "Order shipped", status: NotificationStatus.Queued);
        var second = NotificationsFixtures.CreateNotification(
            subject: "Password reset", status: NotificationStatus.Queued);
        _notifications
            .Setup(x => x.CountAsync(
                NotificationsFixtures.RecipientId, NotificationStatus.Queued,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        _notifications
            .Setup(x => x.GetAllAsync(
                NotificationsFixtures.RecipientId, NotificationStatus.Queued, 2, 5,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Notification> { first, second });

        var result = await handler.Handle(
            new GetNotificationsQuery(
                RecipientId: NotificationsFixtures.RecipientId,
                Status: NotificationStatus.Queued,
                PageNumber: 2,
                PageSize: 5),
            CancellationToken.None);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(5, result.PageSize);
        Assert.Equal(
            new[] { "Order shipped", "Password reset" },
            result.Items.Select(n => n.Subject)); // repository order preserved
        Assert.All(result.Items, n =>
            Assert.Equal(NotificationStatus.Queued, n.Status));

        _notifications.Verify(
            x => x.CountAsync(
                NotificationsFixtures.RecipientId, NotificationStatus.Queued,
                It.IsAny<CancellationToken>()),
            Times.Once);
        _notifications.Verify(
            x => x.GetAllAsync(
                NotificationsFixtures.RecipientId, NotificationStatus.Queued, 2, 5,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
