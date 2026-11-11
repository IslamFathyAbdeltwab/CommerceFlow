using CommerceFlow.Domain.Common;
using CommerceFlow.Domain.Exceptions;

namespace CommerceFlow.Domain.Entities.Wishlists;

// Aggregate root: a customer's wishlist. Unlike the Cart it has no status
// lifecycle (there is nothing to check out or abandon) and no quantities -
// items are saved references to Catalog variants. No money lives here either:
// prices are read from the Catalog when displayed, so a wishlist can never go
// stale. Cross-aggregate references (customer, variant) are ID-only.
public class Wishlist : AggregateRoot
{
    public const int MaxItems = 100;

    private readonly List<WishlistItem> _items = [];

    public Guid CustomerId { get; private set; }

    public IReadOnlyCollection<WishlistItem> Items => _items.AsReadOnly();

    // Derived - not persisted (same pattern as Cart.LineCount).
    public int ItemCount => _items.Count;

    private Wishlist() { } // required by EF Core

    private Wishlist(Guid customerId)
    {
        CustomerId = customerId;
        CreatedAtUtc = UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public static Wishlist Create(Guid customerId)
    {
        if (customerId == Guid.Empty)
            throw new DomainException("Customer ID is required.");

        return new Wishlist(customerId);
    }

    // ---------- Items ----------

    // Saving a variant that is already saved is a no-op that returns the
    // existing item - wishlisting is idempotent by nature.
    public WishlistItem AddItem(Guid productVariantId)
    {
        if (productVariantId == Guid.Empty)
            throw new DomainException("Product variant ID is required.");

        var existing = _items.FirstOrDefault(i => i.ProductVariantId == productVariantId);
        if (existing is not null)
            return existing; // nothing changed -> deliberately no Touch()

        if (_items.Count >= MaxItems)
            throw new DomainException($"A wishlist cannot have more than {MaxItems} items.");

        var item = new WishlistItem(Id, productVariantId);
        _items.Add(item);
        Touch();
        return item;
    }

    public void RemoveItem(Guid itemId)
    {
        var item = FindItem(itemId);
        _items.Remove(item);
        Touch();
    }

    public void Clear()
    {
        _items.Clear();
        Touch();
    }

    // ---------- Helpers ----------

    private WishlistItem FindItem(Guid itemId) =>
        _items.FirstOrDefault(i => i.Id == itemId)
        ?? throw new DomainException("Wishlist item not found.");

    private void Touch() => UpdatedAtUtc = DateTimeOffset.UtcNow;
}
