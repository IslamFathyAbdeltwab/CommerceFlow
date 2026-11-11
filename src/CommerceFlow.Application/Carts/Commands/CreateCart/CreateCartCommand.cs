using MediatR;

namespace CommerceFlow.Application.Carts.Commands.CreateCart;

public sealed record CreateCartCommand(Guid CustomerId) : IRequest<Guid>;
