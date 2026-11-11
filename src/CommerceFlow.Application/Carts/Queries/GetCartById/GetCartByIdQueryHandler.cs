using CommerceFlow.Application.Carts.Interfaces;
using MediatR;

namespace CommerceFlow.Application.Carts.Queries.GetCartById;

public sealed class GetCartByIdQueryHandler(
    ICartRepository carts) : IRequestHandler<GetCartByIdQuery, CartDto?>
{
    public async Task<CartDto?> Handle(GetCartByIdQuery request, CancellationToken cancellationToken)
    {
        var cart = await carts.GetByIdAsync(request.CartId, cancellationToken);
        if (cart is null)
            return null;

        return CartMappings.ToDto(cart);
    }
}
