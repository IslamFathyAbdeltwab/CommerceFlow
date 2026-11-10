using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Orders.Interfaces;
using CommerceFlow.Domain.Entities.Orders;
using CommerceFlow.Domain.Enums;
using MediatR;

namespace CommerceFlow.Application.Orders.Commands.UpdateOrderLineQuantity;

public sealed class UpdateOrderLineQuantityCommandHandler(
    IOrderRepository orders,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateOrderLineQuantityCommand>
{
    public async Task Handle(UpdateOrderLineQuantityCommand request, CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(request.OrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);

        if (order.Status != OrderStatus.Pending)
            throw new ConflictException("Order lines can only be changed while the order is pending.");

        if (order.Lines.All(l => l.Id != request.OrderLineId))
            throw new NotFoundException(nameof(OrderLine), request.OrderLineId);

        order.UpdateLineQuantity(request.OrderLineId, request.Quantity);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
