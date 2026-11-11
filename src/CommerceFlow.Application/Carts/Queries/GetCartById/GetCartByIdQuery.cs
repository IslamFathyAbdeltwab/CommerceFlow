using MediatR;

namespace CommerceFlow.Application.Carts.Queries.GetCartById;

public sealed record GetCartByIdQuery(Guid CartId) : IRequest<CartDto?>;
