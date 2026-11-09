using CommerceFlow.Domain.Common;
using CommerceFlow.Domain.Exceptions;

namespace CommerceFlow.Domain.Entities.Inventory;

// Aggregate root: stock balance for one WarehouseId+ProductVariantId pair.
// Concurrency is handled in Infrastructure via a unique index and RowVersion.
// Domain only guarantees this entity never goes negative in same instance.
public class InventoryItem : AggregateRoot
{
    public Guid WarehouseId { get; private set; }

    // Variant is a Catalog aggregate - reference by ID only.
    public Guid ProductVariantId { get; private set; }

    public int OnHandQuantity { get; private set; }
    public int ReservedQuantity { get; private set; }
    public int ReorderThreshold { get; private set; }

    // Optional optimistic concurrency token (configured in Infrastructure).
    // Keep it here as private bytes to avoid exposing EF concerns in Domain API.
    public byte[]? RowVersion { get; private set; }

    public int AvailableQuantity => OnHandQuantity - ReservedQuantity;

    public bool IsBelowReorderThreshold => AvailableQuantity <= ReorderThreshold;

    private InventoryItem() { } // required by EF Core

    private InventoryItem(
        Guid warehouseId,
        Guid productVariantId,
        int onHandQuantity,
        int reorderThreshold)
    {
        WarehouseId = warehouseId;
        ProductVariantId = productVariantId;
        OnHandQuantity = onHandQuantity;
        ReservedQuantity = 0;
        ReorderThreshold = reorderThreshold;
        CreatedAtUtc = UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public static InventoryItem Create(
        Guid warehouseId,
        Guid productVariantId,
        int onHandQuantity,
        int reorderThreshold = 0)
    {
        if (warehouseId == Guid.Empty)
            throw new DomainException("Warehouse ID is required.");

        if (productVariantId == Guid.Empty)
            throw new DomainException("Product variant ID is required.");

        if (onHandQuantity < 0)
            throw new DomainException("On-hand quantity cannot be negative.");

        if (reorderThreshold < 0)
            throw new DomainException("Reorder threshold cannot be negative.");

        return new InventoryItem(warehouseId, productVariantId, onHandQuantity, reorderThreshold);
    }

    public void ReceiveStock(int quantity, string reason)
    {
        if (quantity <= 0)
            throw new DomainException("Quantity must be positive.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Reason is required for stock movement.");

        OnHandQuantity += quantity;
        Touch();
    }

    public void ShipStock(int quantity, string reason)
    {
        if (quantity <= 0)
            throw new DomainException("Quantity must be positive.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Reason is required for stock movement.");

        if (quantity > AvailableQuantity)
            throw new DomainException("Cannot ship more than available quantity.");

        OnHandQuantity -= quantity;
        Touch();
    }

    public void WriteOffStock(int quantity, string reason)
    {
        if (quantity <= 0)
            throw new DomainException("Quantity must be positive.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Reason is required for stock movement.");

        if (quantity > AvailableQuantity)
            throw new DomainException("Cannot write off more than available quantity.");

        OnHandQuantity -= quantity;
        Touch();
    }

    public void AdjustStock(int quantityChange, string reason)
    {
        if (quantityChange == 0)
            throw new DomainException("Quantity change cannot be zero.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Reason is required for stock movement.");

        if (quantityChange < 0 && -quantityChange > AvailableQuantity)
            throw new DomainException("Adjustment would make available quantity negative.");

        OnHandQuantity += quantityChange;
        Touch();
    }

    public void Reserve(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("Quantity must be positive.");

        if (quantity > AvailableQuantity)
            throw new DomainException("Cannot reserve more than available quantity.");

        ReservedQuantity += quantity;
        Touch();
    }

    public void ReleaseReservation(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("Quantity must be positive.");

        if (quantity > ReservedQuantity)
            throw new DomainException("Cannot release more than reserved quantity.");

        ReservedQuantity -= quantity;
        Touch();
    }

    public void ConsumeReservation(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("Quantity must be positive.");

        if (quantity > ReservedQuantity)
            throw new DomainException("Cannot consume more than reserved quantity.");

        ReservedQuantity -= quantity;
        OnHandQuantity -= quantity;
        Touch();
    }

    public void SetReorderThreshold(int threshold)
    {
        if (threshold < 0)
            throw new DomainException("Reorder threshold cannot be negative.");

        ReorderThreshold = threshold;
        Touch();
    }

    private void Touch() => UpdatedAtUtc = DateTimeOffset.UtcNow;
}
