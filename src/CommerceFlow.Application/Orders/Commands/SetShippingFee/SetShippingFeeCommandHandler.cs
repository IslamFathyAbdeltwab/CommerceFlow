using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Orders.Interfaces;
using CommerceFlow.Domain.Entities.Orders;
using CommerceFlow.Domain.Enums;
using MediatR;

namespace CommerceFlow.Application.Orders.Commands.SetShippingFee;

public sealed class SetShippingFeeCommandHandler(
    IOrderRepository orders,
    IUnitOfWork unitOfWork) : IRequestHandler<SetShippingFeeCommand>
{
    public async Task Handle(SetShippingFeeCommand request, CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(request.OrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);

        if (order.Status != OrderStatus.Pending)
            throw new ConflictException("Totals can only be changed while the order is pending.");

        order.SetShippingFee(request.ShippingFee);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
