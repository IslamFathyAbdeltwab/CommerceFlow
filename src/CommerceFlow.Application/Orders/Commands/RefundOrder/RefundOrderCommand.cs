using MediatR;

namespace CommerceFlow.Application.Orders.Commands.RefundOrder;

public sealed record RefundOrderCommand(Guid OrderId) : IRequest;
