using MediatR;

namespace CommerceFlow.Application.Carts.Commands.ClearCart;

public sealed record ClearCartCommand(Guid CartId) : IRequest;
