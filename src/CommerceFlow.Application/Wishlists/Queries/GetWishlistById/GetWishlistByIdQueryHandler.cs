using CommerceFlow.Application.Wishlists.Interfaces;
using MediatR;

namespace CommerceFlow.Application.Wishlists.Queries.GetWishlistById;

public sealed class GetWishlistByIdQueryHandler(
    IWishlistRepository wishlists) : IRequestHandler<GetWishlistByIdQuery, WishlistDto?>
{
    public async Task<WishlistDto?> Handle(GetWishlistByIdQuery request, CancellationToken cancellationToken)
    {
        var wishlist = await wishlists.GetByIdAsync(request.WishlistId, cancellationToken);
        if (wishlist is null)
            return null;

        return WishlistMappings.ToDto(wishlist);
    }
}
