using CommerceFlow.Application.Carts.Interfaces;
using MediatR;

namespace CommerceFlow.Application.Carts.Queries.GetActiveCartByCustomerId;

public sealed class GetActiveCartByCustomerIdQueryHandler(
    ICartRepository carts) : IRequestHandler<GetActiveCartByCustomerIdQuery, CartDto?>
{
    public async Task<CartDto?> Handle(GetActiveCartByCustomerIdQuery request, CancellationToken cancellationToken)
    {
        var cart = await carts.GetActiveByCustomerIdAsync(request.CustomerId, cancellationToken);
        if (cart is null)
            return null;

        return CartMappings.ToDto(cart);
    }
}
