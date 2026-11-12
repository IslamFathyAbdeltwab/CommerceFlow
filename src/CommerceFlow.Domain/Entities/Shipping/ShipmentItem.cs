using CommerceFlow.Domain.Exceptions;

namespace CommerceFlow.Domain.Entities.Shipping;

// One variant inside a box. Variant is a Catalog aggregate - reference by
// ID only, like OrderLine and InventoryReservationItem do.
public class ShipmentItem
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid ShipmentId { get; private set; }

    // Variant is a Catalog aggregate - reference by ID only.
    public Guid ProductVariantId { get; private set; }

    public int Quantity { get; private set; }

    private ShipmentItem() { } // required by EF Core

    // internal: only the Shipment aggregate root creates items.
    internal ShipmentItem(
        Guid shipmentId,
        Guid productVariantId,
        int quantity)
    {
        if (shipmentId == Guid.Empty)
            throw new DomainException("Shipment ID is required.");

        if (productVariantId == Guid.Empty)
            throw new DomainException("Product variant ID is required.");

        if (quantity <= 0)
            throw new DomainException("Quantity must be positive.");

        ShipmentId = shipmentId;
        ProductVariantId = productVariantId;
        Quantity = quantity;
    }
}
