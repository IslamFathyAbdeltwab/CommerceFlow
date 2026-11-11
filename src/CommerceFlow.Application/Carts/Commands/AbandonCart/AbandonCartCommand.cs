using MediatR;

namespace CommerceFlow.Application.Carts.Commands.AbandonCart;

public sealed record AbandonCartCommand(Guid CartId) : IRequest;
