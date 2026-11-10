using CommerceFlow.Application.Catalog.Products.Queries;
using CommerceFlow.Domain.Enums;
using MediatR;

namespace CommerceFlow.Application.Orders.Queries.GetOrders;

public sealed record GetOrdersQuery(
    Guid? CustomerId = null,
    OrderStatus? Status = null,
    OrderPaymentStatus? PaymentStatus = null,
    string? SearchTerm = null,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PagedResult<OrderListItemDto>>;
