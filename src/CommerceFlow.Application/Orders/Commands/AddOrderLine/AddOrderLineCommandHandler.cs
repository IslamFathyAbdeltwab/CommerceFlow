using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Orders.Interfaces;
using CommerceFlow.Domain.Entities.Orders;
using CommerceFlow.Domain.Enums;
using MediatR;

namespace CommerceFlow.Application.Orders.Commands.AddOrderLine;

public sealed class AddOrderLineCommandHandler(
    IOrderRepository orders,
    IUnitOfWork unitOfWork) : IRequestHandler<AddOrderLineCommand>
{
    public async Task Handle(AddOrderLineCommand request, CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(request.OrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);

        if (order.Status != OrderStatus.Pending)
            throw new ConflictException("Order lines can only be changed while the order is pending.");

        if (order.Lines.Any(l => l.ProductVariantId == request.ProductVariantId))
            throw new ConflictException("Variant is already on this order; update its quantity instead.");

        order.AddLine(
            request.ProductVariantId,
            request.Sku,
            request.ProductName,
            request.UnitPrice,
            request.Quantity);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
