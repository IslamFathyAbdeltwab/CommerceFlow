using CommerceFlow.Application.Notifications.Commands.FailNotification;
using CommerceFlow.Application.Notifications.Commands.MarkNotificationRead;
using CommerceFlow.Application.Notifications.Commands.MarkNotificationSent;
using CommerceFlow.Application.Notifications.Commands.RetryNotification;
using CommerceFlow.Application.Notifications.Commands.SendNotification;
using CommerceFlow.Application.Notifications.Queries.GetNotifications;
using CommerceFlow.Domain.Entities.Notifications;
using CommerceFlow.Domain.Enums;

namespace CommerceFlow.Application.Tests.Notifications;

public sealed class NotificationsValidatorTests
{
    private static string TooLong(int length) => new('x', length);

    // ---------- SendNotification ----------

    [Fact]
    public void Send_WithValidInput_IsValid()
    {
        var result = new SendNotificationCommandValidator()
            .Validate(NotificationsFixtures.ValidSendCommand());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Send_WithEmptyRecipientId_IsInvalid()
    {
        var result = new SendNotificationCommandValidator()
            .Validate(NotificationsFixtures.ValidSendCommand(recipientId: Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(SendNotificationCommand.RecipientId));
    }

    [Fact]
    public void Send_WithUndefinedChannel_IsInvalid()
    {
        var result = new SendNotificationCommandValidator()
            .Validate(NotificationsFixtures.ValidSendCommand(
                channel: (NotificationChannel)99));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(SendNotificationCommand.Channel));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Send_WithEmptySubject_IsInvalid(string subject)
    {
        var result = new SendNotificationCommandValidator()
            .Validate(NotificationsFixtures.ValidSendCommand(subject: subject));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(SendNotificationCommand.Subject));
    }

    [Fact]
    public void Send_WithTooLongSubject_IsInvalid()
    {
        var result = new SendNotificationCommandValidator()
            .Validate(NotificationsFixtures.ValidSendCommand(
                subject: TooLong(Notification.SubjectMaxLength + 1)));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(SendNotificationCommand.Subject));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Send_WithEmptyBody_IsInvalid(string body)
    {
        var result = new SendNotificationCommandValidator()
            .Validate(NotificationsFixtures.ValidSendCommand(body: body));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(SendNotificationCommand.Body));
    }

    [Fact]
    public void Send_WithTooLongBody_IsInvalid()
    {
        var result = new SendNotificationCommandValidator()
            .Validate(NotificationsFixtures.ValidSendCommand(
                body: TooLong(Notification.BodyMaxLength + 1)));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(SendNotificationCommand.Body));
    }

    [Fact]
    public void Send_WithNullAddress_IsValid()
    {
        // null passes on purpose: whether a sender channel needs an address
        // at all is the domain's rule, not a shape question
        var result = new SendNotificationCommandValidator()
            .Validate(NotificationsFixtures.ValidSendCommand(recipientAddress: null));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Send_WithTooLongAddress_IsInvalid()
    {
        var result = new SendNotificationCommandValidator()
            .Validate(NotificationsFixtures.ValidSendCommand(
                recipientAddress: TooLong(Notification.AddressMaxLength + 1)));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(SendNotificationCommand.RecipientAddress));
    }

    [Fact]
    public void Send_OnSenderChannelWithoutAddress_IsValid()
    {
        // documented split: "Email must have an address" is business logic,
        // so the shape validator waves it through for the domain to refuse
        // (DomainException -> 400)
        var result = new SendNotificationCommandValidator()
            .Validate(NotificationsFixtures.ValidSendCommand(
                channel: NotificationChannel.Email, recipientAddress: null));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Send_WithRelatedEntityIdOnly_IsValid()
    {
        // the related-entity pairing is the domain's rule too
        var result = new SendNotificationCommandValidator()
            .Validate(NotificationsFixtures.ValidSendCommand(relatedEntityId: Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Send_WithRelatedEntityTypeOnly_IsValid()
    {
        var result = new SendNotificationCommandValidator()
            .Validate(NotificationsFixtures.ValidSendCommand(relatedEntityType: "Order"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Send_WithTooLongRelatedEntityType_IsInvalid()
    {
        var result = new SendNotificationCommandValidator()
            .Validate(NotificationsFixtures.ValidSendCommand(
                relatedEntityType: TooLong(Notification.RelatedEntityTypeMaxLength + 1)));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(SendNotificationCommand.RelatedEntityType));
    }

    // ---------- MarkNotificationSent ----------

    [Fact]
    public void MarkSent_WithValidInput_IsValid()
    {
        var result = new MarkNotificationSentCommandValidator()
            .Validate(new MarkNotificationSentCommand(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void MarkSent_WithEmptyNotificationId_IsInvalid()
    {
        var result = new MarkNotificationSentCommandValidator()
            .Validate(new MarkNotificationSentCommand(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(MarkNotificationSentCommand.NotificationId));
    }

    // ---------- MarkNotificationRead ----------

    [Fact]
    public void MarkRead_WithValidInput_IsValid()
    {
        var result = new MarkNotificationReadCommandValidator()
            .Validate(new MarkNotificationReadCommand(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void MarkRead_WithEmptyNotificationId_IsInvalid()
    {
        var result = new MarkNotificationReadCommandValidator()
            .Validate(new MarkNotificationReadCommand(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(MarkNotificationReadCommand.NotificationId));
    }

    // ---------- FailNotification ----------

    [Fact]
    public void Fail_WithValidInput_IsValid()
    {
        var result = new FailNotificationCommandValidator()
            .Validate(new FailNotificationCommand(Guid.NewGuid(), "SMTP timeout"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Fail_WithEmptyNotificationId_IsInvalid()
    {
        var result = new FailNotificationCommandValidator()
            .Validate(new FailNotificationCommand(Guid.Empty, "SMTP timeout"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(FailNotificationCommand.NotificationId));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Fail_WithEmptyReason_IsInvalid(string reason)
    {
        // a failure report must say why - the domain repeats this as its
        // safety net (shape-before-transition, same split as all modules)
        var result = new FailNotificationCommandValidator()
            .Validate(new FailNotificationCommand(Guid.NewGuid(), reason));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(FailNotificationCommand.Reason));
    }

    [Fact]
    public void Fail_WithTooLongReason_IsInvalid()
    {
        var result = new FailNotificationCommandValidator()
            .Validate(new FailNotificationCommand(
                Guid.NewGuid(), TooLong(Notification.FailureReasonMaxLength + 1)));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(FailNotificationCommand.Reason));
    }

    // ---------- RetryNotification ----------

    [Fact]
    public void Retry_WithValidInput_IsValid()
    {
        var result = new RetryNotificationCommandValidator()
            .Validate(new RetryNotificationCommand(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Retry_WithEmptyNotificationId_IsInvalid()
    {
        var result = new RetryNotificationCommandValidator()
            .Validate(new RetryNotificationCommand(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(RetryNotificationCommand.NotificationId));
    }

    // ---------- GetNotifications ----------

    [Fact]
    public void GetNotifications_WithDefaults_IsValid()
    {
        var result = new GetNotificationsQueryValidator().Validate(new GetNotificationsQuery());

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1001, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public void GetNotifications_WithPagingOutOfRange_IsInvalid(int pageNumber, int pageSize)
    {
        var result = new GetNotificationsQueryValidator()
            .Validate(new GetNotificationsQuery(PageNumber: pageNumber, PageSize: pageSize));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(GetNotificationsQuery.PageNumber)
            || e.PropertyName == nameof(GetNotificationsQuery.PageSize));
    }

    [Fact]
    public void GetNotifications_WithEmptyRecipientId_IsInvalid()
    {
        // a filter that is set must say something: Guid.Empty matches nothing
        var result = new GetNotificationsQueryValidator()
            .Validate(new GetNotificationsQuery(RecipientId: Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(GetNotificationsQuery.RecipientId));
    }

    [Fact]
    public void GetNotifications_WithNullRecipientId_IsValid()
    {
        // null filter simply means "off" - the whole table
        var result = new GetNotificationsQueryValidator()
            .Validate(new GetNotificationsQuery(RecipientId: null));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void GetNotifications_WithNullStatus_IsValid()
    {
        var result = new GetNotificationsQueryValidator()
            .Validate(new GetNotificationsQuery(Status: null));

        Assert.True(result.IsValid);
    }
}
