using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Wishlists.Interfaces;
using CommerceFlow.Domain.Entities.Wishlists;
using MediatR;

namespace CommerceFlow.Application.Wishlists.Commands.AddWishlistItem;

public sealed class AddWishlistItemCommandHandler(
    IWishlistRepository wishlists,
    IUnitOfWork unitOfWork) : IRequestHandler<AddWishlistItemCommand, Guid>
{
    public async Task<Guid> Handle(AddWishlistItemCommand request, CancellationToken cancellationToken)
    {
        var wishlist = await wishlists.GetByIdAsync(request.WishlistId, cancellationToken)
            ?? throw new NotFoundException(nameof(Wishlist), request.WishlistId);

        // NOTE: Application must not cross into the Catalog aggregate - variant
        // existence belongs to the checkout flow (same decision as Cart items
        // and Order lines, which also reference variants by ID only).
        // The idempotent path wins over the cap: an already-saved variant
        // never hits this pre-check (mirrors the domain's own ordering).
        if (wishlist.ItemCount >= Wishlist.MaxItems
            && wishlist.Items.All(i => i.ProductVariantId != request.ProductVariantId))
            throw new ConflictException($"A wishlist cannot have more than {Wishlist.MaxItems} items.");

        var item = wishlist.AddItem(request.ProductVariantId);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return item.Id;
    }
}
