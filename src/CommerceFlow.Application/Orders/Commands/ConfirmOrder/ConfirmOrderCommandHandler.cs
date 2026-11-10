using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Orders.Interfaces;
using CommerceFlow.Domain.Entities.Orders;
using CommerceFlow.Domain.Enums;
using MediatR;

namespace CommerceFlow.Application.Orders.Commands.ConfirmOrder;

public sealed class ConfirmOrderCommandHandler(
    IOrderRepository orders,
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

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
