using CommerceFlow.Domain.Exceptions;

namespace CommerceFlow.Domain.Entities.Wishlists;

// One saved item on a wishlist. Deliberately holds no quantity and no
// price/SKU/name snapshot (same decision as CartLine): a wishlist must always
// show live catalog data, and snapshots belong to the checkout flow.
public class WishlistItem
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid WishlistId { get; private set; }

    // Variant is a Catalog aggregate - reference by ID only.
    public Guid ProductVariantId { get; private set; }
    public DateTimeOffset AddedAtUtc { get; private set; }

    private WishlistItem() { } // required by EF Core

    // internal: only the Wishlist aggregate creates or removes items.
    internal WishlistItem(Guid wishlistId, Guid productVariantId)
    {
        if (wishlistId == Guid.Empty)
            throw new DomainException("Wishlist ID is required.");

        if (productVariantId == Guid.Empty)
            throw new DomainException("Product variant ID is required.");

        WishlistId = wishlistId;
        ProductVariantId = productVariantId;
        AddedAtUtc = DateTimeOffset.UtcNow;
    }
}
