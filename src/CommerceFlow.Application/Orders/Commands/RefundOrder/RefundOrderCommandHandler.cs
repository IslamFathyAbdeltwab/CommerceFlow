using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Orders.Interfaces;
using CommerceFlow.Domain.Entities.Orders;
using CommerceFlow.Domain.Enums;
using MediatR;

namespace CommerceFlow.Application.Orders.Commands.RefundOrder;

public sealed class RefundOrderCommandHandler(
    IOrderRepository orders,
    IUnitOfWork unitOfWork) : IRequestHandler<RefundOrderCommand>
{
    public async Task Handle(RefundOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(request.OrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);

        if (order.PaymentStatus != OrderPaymentStatus.Paid)
            throw new ConflictException($"Only paid orders can be refunded (current: {order.PaymentStatus}).");

        order.Refund();

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
