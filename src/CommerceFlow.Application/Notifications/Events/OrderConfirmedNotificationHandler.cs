using CommerceFlow.Application.Notifications.Commands.SendNotification;
using CommerceFlow.Application.Orders.Events;
using CommerceFlow.Domain.Enums;
using MediatR;

namespace CommerceFlow.Application.Notifications.Events;

// The Notifications half of the first cross-module flow: Orders announces
// OrderConfirmedIntegrationEvent, this handler turns it into the customer's
// in-app notification. It dispatches the SAME SendNotificationCommand the
// HTTP endpoint uses instead of repeating creation logic - one code path,
// one validator, one set of domain rules. Only the event contract is shared:
// Orders knows nothing about Notifications and could drop this subscriber
// without touching a line of Orders code.
public sealed class OrderConfirmedNotificationHandler(IMediator mediator)
    : INotificationHandler<OrderConfirmedIntegrationEvent>
{
    public async Task Handle(
        OrderConfirmedIntegrationEvent @event,
        CancellationToken cancellationToken)
    {
        await mediator.Send(
            new SendNotificationCommand(
                RecipientId: @event.CustomerId,
                Channel: NotificationChannel.InApp, // feed-only: needs no address
                Subject: $"Order {@event.OrderNumber} confirmed",
                Body: FormattableString.Invariant(
                    $"Your order {@event.OrderNumber} has been confirmed. Total: {@event.GrandTotal:0.##} {@event.Currency}."),
                RelatedEntityType: "Order",
                RelatedEntityId: @event.OrderId),
            cancellationToken);
    }
}
