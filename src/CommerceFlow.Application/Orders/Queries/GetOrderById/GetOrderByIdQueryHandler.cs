using CommerceFlow.Application.Orders.Interfaces;
using MediatR;

namespace CommerceFlow.Application.Orders.Queries.GetOrderById;

public sealed class GetOrderByIdQueryHandler(
    IOrderRepository orders) : IRequestHandler<GetOrderByIdQuery, OrderDto?>
{
    public async Task<OrderDto?> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(request.OrderId, cancellationToken);
        if (order is null)
            return null;

        var address = order.ShippingAddress;

        return new OrderDto(
            order.Id,
            order.OrderNumber,
            order.CustomerId,
            order.Status,
            order.PaymentStatus,
            order.Currency,
            new AddressDto(
                address.FullName,
                address.Line1,
                address.Line2,
                address.City,
                address.State,
                address.PostalCode,
                address.Country,
                address.PhoneNumber),
            order.Subtotal,
            order.ShippingFee,
            order.DiscountAmount,
            order.GrandTotal,
            order.CustomerNote,
            order.CancelReason,
            order.CreatedAtUtc,
            order.PaidAtUtc,
            order.ShippedAtUtc,
            order.DeliveredAtUtc,
            order.CancelledAtUtc,
            order.RefundedAtUtc,
            order.Lines
                .Select(l => new OrderLineDto(
                    l.Id,
                    l.ProductVariantId,
                    l.Sku,
                    l.ProductName,
                    l.UnitPrice,
                    l.Quantity,
                    l.LineTotal))
                .ToList());
    }
}
