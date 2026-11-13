using MediatR;

namespace CommerceFlow.Application.Returns.Commands.ReceiveReturn;

// Warehouse scan: the goods are back with us. Only approved RMAs can be
// received - the empty-box guard stays in the domain as a safety net.
public sealed record ReceiveReturnCommand(Guid ReturnId) : IRequest;
