using MediatR;

namespace CommerceFlow.Application.Wishlists.Commands.RemoveWishlistItem;

public sealed record RemoveWishlistItemCommand(
    Guid WishlistId,
    Guid WishlistItemId) : IRequest;
