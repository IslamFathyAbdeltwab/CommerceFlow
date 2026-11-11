using MediatR;

namespace CommerceFlow.Application.Wishlists.Commands.ClearWishlist;

public sealed record ClearWishlistCommand(Guid WishlistId) : IRequest;
