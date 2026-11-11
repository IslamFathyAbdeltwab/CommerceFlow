using CommerceFlow.Domain.Exceptions;

namespace CommerceFlow.Domain.Entities.Carts;

// One line in a shopping cart. Deliberately holds no price/SKU/name snapshot:
// a cart must always show live catalog data, and snapshots are taken at
// checkout when this line becomes an OrderLine. Quantity is the only
// customer-editable state on the line.
public class CartLine
{
    public const int MaxQuantity = 999;

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid CartId { get; private set; }

    // Variant is a Catalog aggregate - reference by ID only.
    public Guid ProductVariantId { get; private set; }

    public int Quantity { get; private set; }
    public DateTimeOffset AddedAtUtc { get; private set; }

    private CartLine() { } // required by EF Core

    // internal: only the Cart aggregate creates or changes lines.
    internal CartLine(Guid cartId, Guid productVariantId, int quantity)
    {
        if (cartId == Guid.Empty)
            throw new DomainException("Cart ID is required.");

        if (productVariantId == Guid.Empty)
            throw new DomainException("Product variant ID is required.");

        EnsureValidQuantity(quantity);

        CartId = cartId;
        ProductVariantId = productVariantId;
        Quantity = quantity;
        AddedAtUtc = DateTimeOffset.UtcNow;
    }

    internal static void EnsureValidQuantity(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("Quantity must be positive.");

        if (quantity > MaxQuantity)
            throw new DomainException($"Quantity cannot exceed {MaxQuantity} per line.");
    }

    internal void UpdateQuantity(int quantity)
    {
        EnsureValidQuantity(quantity);
        Quantity = quantity;
    }
}
