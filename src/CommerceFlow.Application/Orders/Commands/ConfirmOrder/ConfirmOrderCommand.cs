using MediatR;

namespace CommerceFlow.Application.Orders.Commands.ConfirmOrder;

public sealed record ConfirmOrderCommand(Guid OrderId) : IRequest;
