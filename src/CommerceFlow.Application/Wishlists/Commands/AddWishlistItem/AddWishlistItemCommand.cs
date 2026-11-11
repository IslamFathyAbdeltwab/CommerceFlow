using MediatR;

namespace CommerceFlow.Application.Wishlists.Commands.AddWishlistItem;

// Returns the item id: saving a variant that is already on the wishlist
// returns the existing item instead of creating a duplicate (idempotent).
public sealed record AddWishlistItemCommand(
    Guid WishlistId,
    Guid ProductVariantId) : IRequest<Guid>;
