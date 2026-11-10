using CommerceFlow.Application.Catalog.Products.Queries;
using CommerceFlow.Application.Orders.Interfaces;
using MediatR;

namespace CommerceFlow.Application.Orders.Queries.GetOrders;

public sealed class GetOrdersQueryHandler(
    IOrderRepository orders) : IRequestHandler<GetOrdersQuery, PagedResult<OrderListItemDto>>
{
    public async Task<PagedResult<OrderListItemDto>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
    {
        var totalCount = await orders.CountAsync(
            request.CustomerId,
            request.Status,
            request.PaymentStatus,
            request.SearchTerm,
            cancellationToken);

        var list = await orders.GetAllAsync(
            request.CustomerId,
            request.Status,
            request.PaymentStatus,
            request.SearchTerm,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var items = list.Select(o => new OrderListItemDto(
            o.Id,
            o.OrderNumber,
            o.CustomerId,
            o.Status,
            o.PaymentStatus,
            o.Currency,
            o.GrandTotal,
            o.CreatedAtUtc)).ToList();

        return new PagedResult<OrderListItemDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}
