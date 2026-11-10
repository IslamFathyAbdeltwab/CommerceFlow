using MediatR;

namespace CommerceFlow.Application.Orders.Commands.ShipOrder;

public sealed record ShipOrderCommand(Guid OrderId) : IRequest;
