using CommerceFlow.Domain.Exceptions;

namespace CommerceFlow.Domain.Entities.Orders;

// One line on an order. SKU, product name and unit price are snapshots taken
// at checkout: later catalog price/name changes must never alter an existing order.
public class OrderLine
{
    public const int SkuMaxLength = 100;
    public const int ProductNameMaxLength = 300;
    public const int MaxQuantity = 999;

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid OrderId { get; private set; }

    // Variant is a Catalog aggregate - reference by ID only.
    public Guid ProductVariantId { get; private set; }

    public string Sku { get; private set; } = string.Empty;
    public string ProductName { get; private set; } = string.Empty;
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }

    public decimal LineTotal => UnitPrice * Quantity; // derived - not persisted

    private OrderLine() { } // required by EF Core

    // internal: only the Order aggregate root creates or changes lines.
    internal OrderLine(
        Guid orderId,
        Guid productVariantId,
        string sku,
        string productName,
        decimal unitPrice,
        int quantity)
    {
        if (orderId == Guid.Empty)
            throw new DomainException("Order ID is required.");

        if (productVariantId == Guid.Empty)
            throw new DomainException("Product variant ID is required.");

        var trimmedSku = sku?.Trim() ?? string.Empty;
        if (trimmedSku.Length == 0)
            throw new DomainException("SKU is required.");
        if (trimmedSku.Length > SkuMaxLength)
            throw new DomainException($"SKU cannot exceed {SkuMaxLength} characters.");

        var trimmedName = productName?.Trim() ?? string.Empty;
        if (trimmedName.Length == 0)
            throw new DomainException("Product name is required.");
        if (trimmedName.Length > ProductNameMaxLength)
            throw new DomainException($"Product name cannot exceed {ProductNameMaxLength} characters.");

        if (unitPrice < 0)
            throw new DomainException("Unit price cannot be negative.");

        EnsureValidQuantity(quantity);

        OrderId = orderId;
        ProductVariantId = productVariantId;
        Sku = trimmedSku;
        ProductName = trimmedName;
        UnitPrice = unitPrice;
        Quantity = quantity;
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
