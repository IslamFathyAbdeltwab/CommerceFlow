using MediatR;

namespace CommerceFlow.Application.Orders.Commands.RemoveOrderLine;

public sealed record RemoveOrderLineCommand(
    Guid OrderId,
    Guid OrderLineId) : IRequest;
