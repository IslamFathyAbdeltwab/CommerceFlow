using CommerceFlow.Domain.Entities.Wishlists;
using CommerceFlow.Domain.Exceptions;

namespace CommerceFlow.Domain.Tests.Wishlists;

public class WishlistTests
{
    private static readonly Guid CustomerId = Guid.NewGuid();

    private static Wishlist NewWishlist() => Wishlist.Create(CustomerId);

    // ---------- Create ----------

    [Fact]
    public void Create_ValidData_CreatesEmptyWishlistForCustomer()
    {
        var wishlist = Wishlist.Create(CustomerId);

        Assert.Equal(CustomerId, wishlist.CustomerId);
        Assert.Empty(wishlist.Items);
        Assert.Equal(0, wishlist.ItemCount);
        Assert.NotEqual(Guid.Empty, wishlist.Id);
    }

    [Fact]
    public void Create_SetsAuditTimestamps()
    {
        var wishlist = Wishlist.Create(CustomerId);

        Assert.NotEqual(default, wishlist.CreatedAtUtc);
        Assert.NotEqual(default, wishlist.UpdatedAtUtc);
        Assert.True(wishlist.UpdatedAtUtc >= wishlist.CreatedAtUtc);
    }

    [Fact]
    public void Create_EmptyCustomerId_Throws()
    {
        Assert.Throws<DomainException>(() => Wishlist.Create(Guid.Empty));
    }

    // ---------- Derived count ----------

    [Fact]
    public void ItemCount_ReflectsItemState()
    {
        var wishlist = NewWishlist();
        wishlist.AddItem(Guid.NewGuid());
        wishlist.AddItem(Guid.NewGuid());

        Assert.Equal(2, wishlist.ItemCount);

        wishlist.RemoveItem(wishlist.Items.First().Id);

        Assert.Equal(1, wishlist.ItemCount);
    }

    // ---------- Idempotent add ----------

    [Fact]
    public void AddItem_DuplicateVariant_ReturnsSameItemWithoutDuplicating()
    {
        var wishlist = NewWishlist();
        var variantId = Guid.NewGuid();

        var first = wishlist.AddItem(variantId);
        var second = wishlist.AddItem(variantId);

        Assert.Same(first, second);
        Assert.Single(wishlist.Items);
        Assert.Equal(1, wishlist.ItemCount);
    }

    [Fact]
    public void AddItem_DuplicateVariant_DoesNotTouchUpdatedAt()
    {
        var wishlist = NewWishlist();
        var variantId = Guid.NewGuid();
        wishlist.AddItem(variantId);
        var updatedAt = wishlist.UpdatedAtUtc;

        wishlist.AddItem(variantId); // no-op

        Assert.Equal(updatedAt, wishlist.UpdatedAtUtc);
    }

    // ---------- Max items ----------

    [Fact]
    public void AddItem_ExceedingMaxItems_ThrowsAndKeepsCount()
    {
        var wishlist = NewWishlist();
        for (var i = 0; i < Wishlist.MaxItems; i++)
            wishlist.AddItem(Guid.NewGuid());

        Assert.Equal(Wishlist.MaxItems, wishlist.ItemCount);

        Assert.Throws<DomainException>(() => wishlist.AddItem(Guid.NewGuid()));
        Assert.Equal(Wishlist.MaxItems, wishlist.ItemCount);
    }

    [Fact]
    public void AddItem_AtMaxItems_WithExistingVariant_DoesNotThrow()
    {
        var wishlist = NewWishlist();
        var variantId = Guid.NewGuid();
        var first = wishlist.AddItem(variantId);
        for (var i = 1; i < Wishlist.MaxItems; i++)
            wishlist.AddItem(Guid.NewGuid());

        var again = wishlist.AddItem(variantId); // already saved: idempotent path wins

        Assert.Same(first, again);
        Assert.Equal(Wishlist.MaxItems, wishlist.ItemCount);
    }

    // ---------- Clear ----------

    [Fact]
    public void Clear_RemovesAllItems()
    {
        var wishlist = NewWishlist();
        wishlist.AddItem(Guid.NewGuid());
        wishlist.AddItem(Guid.NewGuid());

        wishlist.Clear();

        Assert.Empty(wishlist.Items);
        Assert.Equal(0, wishlist.ItemCount);
    }

    [Fact]
    public void Clear_OnEmptyWishlist_IsNoOp()
    {
        var wishlist = NewWishlist();

        wishlist.Clear();

        Assert.Empty(wishlist.Items);
        Assert.Equal(CustomerId, wishlist.CustomerId);
    }
}
