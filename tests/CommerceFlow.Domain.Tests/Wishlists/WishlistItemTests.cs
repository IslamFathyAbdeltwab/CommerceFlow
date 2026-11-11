using CommerceFlow.Domain.Entities.Wishlists;
using CommerceFlow.Domain.Exceptions;

namespace CommerceFlow.Domain.Tests.Wishlists;

// WishlistItem has an internal constructor on purpose - items can only be
// created through the Wishlist aggregate, so every test here goes through
// Wishlist (same pattern as CartLineTests).
public class WishlistItemTests
{
    private static readonly Guid CustomerId = Guid.NewGuid();

    private static Wishlist NewWishlist() => Wishlist.Create(CustomerId);

    // ---------- AddItem ----------

    [Fact]
    public void AddItem_ValidData_CreatesItemWithWishlistReferenceAndTimestamp()
    {
        var wishlist = NewWishlist();
        var variantId = Guid.NewGuid();

        var item = wishlist.AddItem(variantId);

        Assert.Equal(wishlist.Id, item.WishlistId);
        Assert.Equal(variantId, item.ProductVariantId);
        Assert.NotEqual(Guid.Empty, item.Id);
        Assert.NotEqual(default, item.AddedAtUtc);
        Assert.True((DateTimeOffset.UtcNow - item.AddedAtUtc) < TimeSpan.FromMinutes(1));
        Assert.Single(wishlist.Items);
    }

    [Fact]
    public void AddItem_DifferentVariants_GenerateDistinctItems()
    {
        var wishlist = NewWishlist();

        var first = wishlist.AddItem(Guid.NewGuid());
        var second = wishlist.AddItem(Guid.NewGuid());

        Assert.NotEqual(first.Id, second.Id);
        Assert.Equal(2, wishlist.ItemCount);
    }

    [Fact]
    public void AddItem_ExistingVariant_ReturnsSameInstance()
    {
        var wishlist = NewWishlist();
        var variantId = Guid.NewGuid();

        var first = wishlist.AddItem(variantId);
        var second = wishlist.AddItem(variantId);

        Assert.Same(first, second);
        Assert.Equal(first.AddedAtUtc, second.AddedAtUtc); // original timestamp kept
    }

    [Fact]
    public void AddItem_EmptyVariantId_ThrowsAndKeepsWishlistEmpty()
    {
        var wishlist = NewWishlist();

        Assert.Throws<DomainException>(() => wishlist.AddItem(Guid.Empty));
        Assert.Empty(wishlist.Items);
    }

    [Fact]
    public void AddItem_AtMaxItems_ThrowsAndKeepsItemCount()
    {
        var wishlist = NewWishlist();
        for (var i = 0; i < Wishlist.MaxItems; i++)
            wishlist.AddItem(Guid.NewGuid());

        Assert.Throws<DomainException>(() => wishlist.AddItem(Guid.NewGuid()));
        Assert.Equal(Wishlist.MaxItems, wishlist.ItemCount);
    }

    // ---------- RemoveItem ----------

    [Fact]
    public void RemoveItem_Valid_RemovesOnlyThatItem()
    {
        var wishlist = NewWishlist();
        var first = wishlist.AddItem(Guid.NewGuid());
        var second = wishlist.AddItem(Guid.NewGuid());

        wishlist.RemoveItem(first.Id);

        var remaining = Assert.Single(wishlist.Items);
        Assert.Equal(second.Id, remaining.Id);
        Assert.Equal(1, wishlist.ItemCount);
    }

    [Fact]
    public void RemoveItem_UnknownItem_ThrowsAndKeepsItems()
    {
        var wishlist = NewWishlist();
        wishlist.AddItem(Guid.NewGuid());

        Assert.Throws<DomainException>(() => wishlist.RemoveItem(Guid.NewGuid()));
        Assert.Single(wishlist.Items);
    }

    [Fact]
    public void RemoveItem_ThenReAddSameVariant_CreatesNewItem()
    {
        var wishlist = NewWishlist();
        var variantId = Guid.NewGuid();
        var original = wishlist.AddItem(variantId);

        wishlist.RemoveItem(original.Id);
        var reAdded = wishlist.AddItem(variantId);

        Assert.NotSame(original, reAdded); // a fresh save, not a resurrection
        Assert.Equal(variantId, reAdded.ProductVariantId);
        Assert.Single(wishlist.Items);
    }
}
