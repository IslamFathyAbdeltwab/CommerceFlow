using MediatR;

namespace CommerceFlow.Application.Orders.Commands.UpdateOrderLineQuantity;

public sealed record UpdateOrderLineQuantityCommand(
    Guid OrderId,
    Guid OrderLineId,
    int Quantity) : IRequest;
