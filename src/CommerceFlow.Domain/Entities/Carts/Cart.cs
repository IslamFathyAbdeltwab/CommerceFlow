using CommerceFlow.Domain.Common;
using CommerceFlow.Domain.Enums;
using CommerceFlow.Domain.Exceptions;

namespace CommerceFlow.Domain.Entities.Carts;

// Aggregate root: a customer's shopping cart. Owns its lines.
// No money lives here - prices are read from the Catalog at checkout time,
// so a cart can never go stale and never needs a price snapshot.
// Cross-aggregate references (customer, variant) are ID-only.
public class Cart : AggregateRoot
{
    public const int MaxLines = 100;

    private readonly List<CartLine> _lines = [];

    public Guid CustomerId { get; private set; }
    public CartStatus Status { get; private set; }

    public IReadOnlyCollection<CartLine> Lines => _lines.AsReadOnly();

    // Derived - not persisted (same pattern as InventoryItem.AvailableQuantity).
    public int LineCount => _lines.Count;
    public int TotalQuantity => _lines.Sum(l => l.Quantity);

    private Cart() { } // required by EF Core

    private Cart(Guid customerId)
    {
        CustomerId = customerId;
        Status = CartStatus.Active;
        CreatedAtUtc = UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public static Cart Create(Guid customerId)
    {
        if (customerId == Guid.Empty)
            throw new DomainException("Customer ID is required.");

        return new Cart(customerId);
    }

    // ---------- Lines (editable only while Active) ----------

    public CartLine AddItem(Guid productVariantId, int quantity)
    {
        EnsureActive();

        if (productVariantId == Guid.Empty)
            throw new DomainException("Product variant ID is required.");

        CartLine.EnsureValidQuantity(quantity);

        var existing = _lines.FirstOrDefault(l => l.ProductVariantId == productVariantId);
        if (existing is not null)
        {
            // Adding the same variant again just bumps its quantity, like a real cart.
            existing.UpdateQuantity(existing.Quantity + quantity);
            Touch();
            return existing;
        }

        if (_lines.Count >= MaxLines)
            throw new DomainException($"A cart cannot have more than {MaxLines} lines.");

        var line = new CartLine(Id, productVariantId, quantity);
        _lines.Add(line);
        Touch();
        return line;
    }

    public void ChangeQuantity(Guid lineId, int quantity)
    {
        EnsureActive();

        var line = FindLine(lineId);
        line.UpdateQuantity(quantity);
        Touch();
    }

    public void RemoveItem(Guid lineId)
    {
        EnsureActive();

        var line = FindLine(lineId);
        _lines.Remove(line);
        Touch();
    }

    public void Clear()
    {
        EnsureActive();

        _lines.Clear();
        Touch();
    }

    // ---------- Lifecycle ----------

    public void MarkCheckedOut()
    {
        EnsureStatus(CartStatus.Active, "check out");

        if (_lines.Count == 0)
            throw new DomainException("Cannot check out an empty cart.");

        Status = CartStatus.CheckedOut;
        Touch();
    }

    public void MarkAbandoned()
    {
        EnsureStatus(CartStatus.Active, "abandon");

        Status = CartStatus.Abandoned;
        Touch();
    }

    // ---------- Helpers ----------

    private CartLine FindLine(Guid lineId) =>
        _lines.FirstOrDefault(l => l.Id == lineId)
        ?? throw new DomainException("Cart line not found.");

    private void EnsureActive()
    {
        if (Status != CartStatus.Active)
            throw new DomainException("Cart items can only be changed while the cart is active.");
    }

    private void EnsureStatus(CartStatus expected, string action)
    {
        if (Status != expected)
            throw new DomainException($"Cannot {action} a cart in {Status} status.");
    }

    private void Touch() => UpdatedAtUtc = DateTimeOffset.UtcNow;
}
