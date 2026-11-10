using MediatR;

namespace CommerceFlow.Application.Orders.Commands.DeliverOrder;

public sealed record DeliverOrderCommand(Guid OrderId) : IRequest;
