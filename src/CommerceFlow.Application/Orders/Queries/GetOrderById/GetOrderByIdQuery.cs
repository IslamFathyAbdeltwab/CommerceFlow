using MediatR;

namespace CommerceFlow.Application.Orders.Queries.GetOrderById;

public sealed record GetOrderByIdQuery(Guid OrderId) : IRequest<OrderDto?>;
