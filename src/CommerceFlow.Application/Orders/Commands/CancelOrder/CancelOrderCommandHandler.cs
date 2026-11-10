using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Orders.Interfaces;
using CommerceFlow.Domain.Entities.Orders;
using CommerceFlow.Domain.Enums;
using MediatR;

namespace CommerceFlow.Application.Orders.Commands.CancelOrder;

public sealed class CancelOrderCommandHandler(
    IOrderRepository orders,
    IUnitOfWork unitOfWork) : IRequestHandler<CancelOrderCommand>
{
    public async Task Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(request.OrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);

        if (order.Status is not (OrderStatus.Pending or OrderStatus.Confirmed))
            throw new ConflictException($"Only pending or confirmed orders can be cancelled (current: {order.Status}).");

        order.Cancel(request.Reason);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
