using CommerceFlow.Domain.Exceptions;

namespace CommerceFlow.Domain.Entities.Inventory;

public class InventoryReservationItem
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid InventoryReservationId { get; private set; }
    public Guid WarehouseId { get; private set; }

    // Variant is a Catalog aggregate - reference by ID only.
    public Guid ProductVariantId { get; private set; }

    public int Quantity { get; private set; }

    private InventoryReservationItem() { } // required by EF Core

    // internal: only the InventoryReservation aggregate root creates items.
    internal InventoryReservationItem(
        Guid inventoryReservationId,
        Guid warehouseId,
        Guid productVariantId,
        int quantity)
    {
        if (inventoryReservationId == Guid.Empty)
            throw new DomainException("Inventory reservation ID is required.");

        if (warehouseId == Guid.Empty)
            throw new DomainException("Warehouse ID is required.");

        if (productVariantId == Guid.Empty)
            throw new DomainException("Product variant ID is required.");

        if (quantity <= 0)
            throw new DomainException("Quantity must be positive.");

        InventoryReservationId = inventoryReservationId;
        WarehouseId = warehouseId;
        ProductVariantId = productVariantId;
        Quantity = quantity;
    }
}
