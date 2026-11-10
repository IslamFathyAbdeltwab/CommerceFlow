using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Orders.Interfaces;
using CommerceFlow.Domain.Entities.Orders;
using CommerceFlow.Domain.Enums;
using MediatR;

namespace CommerceFlow.Application.Orders.Commands.ShipOrder;

public sealed class ShipOrderCommandHandler(
    IOrderRepository orders,
    IUnitOfWork unitOfWork) : IRequestHandler<ShipOrderCommand>
{
    public async Task Handle(ShipOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(request.OrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);

        if (order.Status != OrderStatus.Confirmed)
            throw new ConflictException($"Only confirmed orders can be shipped (current: {order.Status}).");

        order.Ship();

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
