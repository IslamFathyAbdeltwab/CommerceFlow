using MediatR;

namespace CommerceFlow.Application.Carts.Commands.ChangeCartItemQuantity;

public sealed record ChangeCartItemQuantityCommand(
    Guid CartId,
    Guid CartLineId,
    int Quantity) : IRequest;
