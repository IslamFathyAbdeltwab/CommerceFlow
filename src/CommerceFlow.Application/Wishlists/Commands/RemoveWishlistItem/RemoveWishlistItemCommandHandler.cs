using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Wishlists.Interfaces;
using CommerceFlow.Domain.Entities.Wishlists;
using MediatR;

namespace CommerceFlow.Application.Wishlists.Commands.RemoveWishlistItem;

public sealed class RemoveWishlistItemCommandHandler(
    IWishlistRepository wishlists,
    IUnitOfWork unitOfWork) : IRequestHandler<RemoveWishlistItemCommand>
{
    public async Task Handle(RemoveWishlistItemCommand request, CancellationToken cancellationToken)
    {
        var wishlist = await wishlists.GetByIdAsync(request.WishlistId, cancellationToken)
            ?? throw new NotFoundException(nameof(Wishlist), request.WishlistId);

        if (wishlist.Items.All(i => i.Id != request.WishlistItemId))
            throw new NotFoundException(nameof(WishlistItem), request.WishlistItemId);

        wishlist.RemoveItem(request.WishlistItemId);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
