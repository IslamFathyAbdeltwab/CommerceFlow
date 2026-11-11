using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Wishlists.Interfaces;
using CommerceFlow.Domain.Entities.Wishlists;
using MediatR;

namespace CommerceFlow.Application.Wishlists.Commands.ClearWishlist;

public sealed class ClearWishlistCommandHandler(
    IWishlistRepository wishlists,
    IUnitOfWork unitOfWork) : IRequestHandler<ClearWishlistCommand>
{
    public async Task Handle(ClearWishlistCommand request, CancellationToken cancellationToken)
    {
        var wishlist = await wishlists.GetByIdAsync(request.WishlistId, cancellationToken)
            ?? throw new NotFoundException(nameof(Wishlist), request.WishlistId);

        wishlist.Clear();

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
