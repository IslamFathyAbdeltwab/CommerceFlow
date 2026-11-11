using CommerceFlow.Application.Wishlists.Interfaces;
using MediatR;

namespace CommerceFlow.Application.Wishlists.Queries.GetWishlistByCustomerId;

public sealed class GetWishlistByCustomerIdQueryHandler(
    IWishlistRepository wishlists) : IRequestHandler<GetWishlistByCustomerIdQuery, WishlistDto?>
{
    public async Task<WishlistDto?> Handle(GetWishlistByCustomerIdQuery request, CancellationToken cancellationToken)
    {
        var wishlist = await wishlists.GetByCustomerIdAsync(request.CustomerId, cancellationToken);
        if (wishlist is null)
            return null;

        return WishlistMappings.ToDto(wishlist);
    }
}
