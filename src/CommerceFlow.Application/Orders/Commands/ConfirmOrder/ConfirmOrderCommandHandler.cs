using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Orders.Events;
using CommerceFlow.Application.Orders.Interfaces;
using CommerceFlow.Domain.Entities.Orders;
using CommerceFlow.Domain.Enums;
using MediatR;

namespace CommerceFlow.Application.Orders.Commands.ConfirmOrder;

public sealed class ConfirmOrderCommandHandler(
    IOrderRepository orders,
    IMediator mediator,
    IUnitOfWork unitOfWork) : IRequestHandler<ConfirmOrderCommand>
{
    public async Task Handle(ConfirmOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(request.OrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);

        if (order.Status != OrderStatus.Pending)
            throw new ConflictException($"Only pending orders can be confirmed (current: {order.Status}).");

        if (order.Lines.Count == 0)
            throw new ConflictException("Cannot confirm an order without lines.");

        order.Confirm();

        // In-process integration event: Orders announces the transition once
        // (Pending -> Confirmed is not repeatable, so a re-confirm 409s above
        // and can never double-notify). The Notifications module subscribes
        // and creates the customer's notification through this same unit of
        // work: the order change and the notification row commit in one save,
        // and a subscriber that throws leaves the order Pending.
        await mediator.Publish(
            new OrderConfirmedIntegrationEvent(
                order.Id,
                order.CustomerId,
                order.OrderNumber,
                order.GrandTotal,
                order.Currency),
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
