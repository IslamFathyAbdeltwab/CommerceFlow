using MediatR;

namespace CommerceFlow.Application.Wishlists.Queries.GetWishlistById;

public sealed record GetWishlistByIdQuery(Guid WishlistId) : IRequest<WishlistDto?>;
