using CommerceFlow.Domain.Entities.Wishlists;

namespace CommerceFlow.Application.Tests.Wishlists;

// Shared factories for building Wishlist instances in application tests.
internal static class WishlistFixtures
{
    public static readonly Guid CustomerId = Guid.Parse("33331111-3333-3333-3333-333333333333");

    public static Wishlist CreateWishlist(int items = 0, Guid? customerId = null)
    {
        var wishlist = Wishlist.Create(customerId ?? CustomerId);

        for (var i = 0; i < items; i++)
            wishlist.AddItem(Guid.NewGuid());

        return wishlist;
    }

    // Wishlist with a single item whose id is known, for item-level command tests.
    public static (Wishlist Wishlist, WishlistItem Item) WishlistWithItem(Guid? variantId = null)
    {
        var wishlist = Wishlist.Create(CustomerId);
        var item = wishlist.AddItem(variantId ?? Guid.NewGuid());
        return (wishlist, item);
    }

    // Wishlist already at MaxItems (100), with one known variant,
    // for the item-limit pre-check tests.
    public static (Wishlist Wishlist, Guid VariantId) WishlistAtMaxItems()
    {
        var wishlist = Wishlist.Create(CustomerId);
        var variantId = Guid.NewGuid();
        wishlist.AddItem(variantId);

        for (var i = 1; i < Wishlist.MaxItems; i++)
            wishlist.AddItem(Guid.NewGuid());

        return (wishlist, variantId);
    }
}
