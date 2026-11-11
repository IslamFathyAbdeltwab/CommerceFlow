using MediatR;

namespace CommerceFlow.Application.Carts.Commands.RemoveCartItem;

public sealed record RemoveCartItemCommand(
    Guid CartId,
    Guid CartLineId) : IRequest;
