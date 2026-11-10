using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Orders.Interfaces;
using CommerceFlow.Domain.Entities.Orders;
using MediatR;

namespace CommerceFlow.Application.Orders.Commands.PlaceOrder;

public sealed class PlaceOrderCommandHandler(
    IOrderRepository orders,
    IUnitOfWork unitOfWork) : IRequestHandler<PlaceOrderCommand, Guid>
{
    public async Task<Guid> Handle(PlaceOrderCommand request, CancellationToken cancellationToken)
    {
        var addressDto = request.ShippingAddress;

        var address = AddressSnapshot.Create(
            addressDto.FullName,
            addressDto.Line1,
            addressDto.City,
            addressDto.PostalCode,
            addressDto.Country,
            addressDto.Line2,
            addressDto.State,
            addressDto.PhoneNumber);

        var order = Order.Create(request.CustomerId, address, request.Currency, request.CustomerNote);

        foreach (var line in request.Lines)
            order.AddLine(line.ProductVariantId, line.Sku, line.ProductName, line.UnitPrice, line.Quantity);

        if (request.ShippingFee != 0)
            order.SetShippingFee(request.ShippingFee);

        if (request.DiscountAmount != 0)
            order.ApplyDiscount(request.DiscountAmount);

        await orders.AddAsync(order, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return order.Id;
    }
}
