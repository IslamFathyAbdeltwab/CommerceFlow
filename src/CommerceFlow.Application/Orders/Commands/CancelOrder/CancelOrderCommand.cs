using MediatR;

namespace CommerceFlow.Application.Orders.Commands.CancelOrder;

public sealed record CancelOrderCommand(
    Guid OrderId,
    string? Reason = null) : IRequest;
