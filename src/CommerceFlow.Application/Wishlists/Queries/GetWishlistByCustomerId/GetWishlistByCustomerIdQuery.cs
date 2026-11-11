using MediatR;

namespace CommerceFlow.Application.Wishlists.Queries.GetWishlistByCustomerId;

// Returns null when the customer has no wishlist, so the client can
// distinguish "no wishlist yet" from "wishlist not found".
public sealed record GetWishlistByCustomerIdQuery(Guid CustomerId) : IRequest<WishlistDto?>;
