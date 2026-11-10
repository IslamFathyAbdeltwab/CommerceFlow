using MediatR;

namespace CommerceFlow.Application.Orders.Commands.MarkOrderAsPaid;

public sealed record MarkOrderAsPaidCommand(Guid OrderId) : IRequest;
