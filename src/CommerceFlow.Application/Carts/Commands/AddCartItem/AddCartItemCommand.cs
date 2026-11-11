using MediatR;

namespace CommerceFlow.Application.Carts.Commands.AddCartItem;

// Returns the line id: adding a variant that is already in the cart merges
// into that existing line instead of creating a duplicate.
public sealed record AddCartItemCommand(
    Guid CartId,
    Guid ProductVariantId,
    int Quantity) : IRequest<Guid>;
