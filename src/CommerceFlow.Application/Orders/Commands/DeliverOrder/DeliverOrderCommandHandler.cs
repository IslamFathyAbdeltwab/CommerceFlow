using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Orders.Interfaces;
using CommerceFlow.Domain.Entities.Orders;
using CommerceFlow.Domain.Enums;
using MediatR;

namespace CommerceFlow.Application.Orders.Commands.DeliverOrder;

public sealed class DeliverOrderCommandHandler(
    IOrderRepository orders,
    IUnitOfWork unitOfWork) : IRequestHandler<DeliverOrderCommand>
{
    public async Task Handle(DeliverOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(request.OrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);

        if (order.Status != OrderStatus.Shipped)
            throw new ConflictException($"Only shipped orders can be delivered (current: {order.Status}).");

        order.Deliver();

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
