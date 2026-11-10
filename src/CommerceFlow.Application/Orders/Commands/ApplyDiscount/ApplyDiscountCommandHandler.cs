using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Orders.Interfaces;
using CommerceFlow.Domain.Entities.Orders;
using CommerceFlow.Domain.Enums;
using MediatR;

namespace CommerceFlow.Application.Orders.Commands.ApplyDiscount;

public sealed class ApplyDiscountCommandHandler(
    IOrderRepository orders,
    IUnitOfWork unitOfWork) : IRequestHandler<ApplyDiscountCommand>
{
    public async Task Handle(ApplyDiscountCommand request, CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(request.OrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);

        if (order.Status != OrderStatus.Pending)
            throw new ConflictException("Totals can only be changed while the order is pending.");

        if (request.DiscountAmount > order.Subtotal + order.ShippingFee)
            throw new ConflictException("Discount cannot exceed the order total.");

        order.ApplyDiscount(request.DiscountAmount);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
