using CommerceFlow.Application.Notifications.Commands.SendNotification;
using CommerceFlow.Application.Notifications.Events;
using CommerceFlow.Application.Orders.Events;
using CommerceFlow.Domain.Enums;
using MediatR;
using Moq;

namespace CommerceFlow.Application.Tests.Notifications;

// Subscriber side of the Order -> Notification integration event: Orders
// announces, this handler reacts by dispatching the normal send command.
public sealed class OrderConfirmedNotificationHandlerTests
{
    private readonly Mock<IMediator> _mediator = new();

    private static OrderConfirmedIntegrationEvent SomeEvent() =>
        new(
            OrderId: Guid.NewGuid(),
            CustomerId: Guid.NewGuid(),
            OrderNumber: "CF-1042",
            GrandTotal: 59.97m,
            Currency: "USD");

    [Fact]
    public async Task Handle_SendsInAppNotificationLinkedToTheOrder()
    {
        _mediator
            .Setup(x => x.Send(It.IsAny<SendNotificationCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());
        var handler = new OrderConfirmedNotificationHandler(_mediator.Object);
        var @event = SomeEvent();

        await handler.Handle(@event, CancellationToken.None);

        _mediator.Verify(
            x => x.Send(
                It.Is<SendNotificationCommand>(c =>
                    c.RecipientId == @event.CustomerId &&
                    c.Channel == NotificationChannel.InApp &&
                    c.Subject == "Order CF-1042 confirmed" &&
                    c.Body == "Your order CF-1042 has been confirmed. Total: 59.97 USD." &&
                    c.RecipientAddress == null && // in-app needs no address
                    c.RelatedEntityType == "Order" &&
                    c.RelatedEntityId == @event.OrderId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenSendRefuses_PropagatesInsteadOfSwallowing()
    {
        _mediator
            .Setup(x => x.Send(It.IsAny<SendNotificationCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("notification rejected"));
        var handler = new OrderConfirmedNotificationHandler(_mediator.Object);

        // no swallowing: the exception bubbles to the confirming request, so
        // the SaveChanges behind it never runs and the order stays Pending
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(SomeEvent(), CancellationToken.None));

        Assert.Equal("notification rejected", exception.Message);
    }
}
