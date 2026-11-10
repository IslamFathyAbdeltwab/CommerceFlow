using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Orders.Interfaces;
using CommerceFlow.Domain.Entities.Orders;
using CommerceFlow.Domain.Enums;
using MediatR;

namespace CommerceFlow.Application.Orders.Commands.MarkOrderAsPaid;

public sealed class MarkOrderAsPaidCommandHandler(
    IOrderRepository orders,
    IUnitOfWork unitOfWork) : IRequestHandler<MarkOrderAsPaidCommand>
{
    public async Task Handle(MarkOrderAsPaidCommand request, CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(request.OrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);

        if (order.PaymentStatus != OrderPaymentStatus.Unpaid)
            throw new ConflictException($"Order payment is already {order.PaymentStatus}.");

        if (order.Status is not (OrderStatus.Pending or OrderStatus.Confirmed))
            throw new ConflictException("Only pending or confirmed orders can be marked as paid.");

        order.MarkAsPaid();

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
