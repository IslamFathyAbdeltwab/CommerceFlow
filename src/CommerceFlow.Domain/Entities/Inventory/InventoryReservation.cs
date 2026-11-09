using CommerceFlow.Domain.Common;
using CommerceFlow.Domain.Enums;
using CommerceFlow.Domain.Exceptions;

namespace CommerceFlow.Domain.Entities.Inventory;

// Holds stock for an order while it awaits the next step.
// It references OrderId by ID only (Order is another aggregate).
public class InventoryReservation : AggregateRoot
{
    private readonly List<InventoryReservationItem> _items = [];

    public Guid OrderId { get; private set; }
    public InventoryReservationStatus Status { get; private set; }
    public DateTimeOffset? ExpiresAtUtc { get; private set; }
    public DateTimeOffset? ReleasedAtUtc { get; private set; }

    public IReadOnlyCollection<InventoryReservationItem> Items => _items.AsReadOnly();

    private InventoryReservation() { } // required by EF Core

    private InventoryReservation(Guid orderId, DateTimeOffset? expiresAtUtc)
    {
        OrderId = orderId;
        Status = InventoryReservationStatus.Active;
        ExpiresAtUtc = expiresAtUtc;
        CreatedAtUtc = UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public static InventoryReservation Create(Guid orderId, DateTimeOffset? expiresAtUtc = null)
    {
        if (orderId == Guid.Empty)
            throw new DomainException("Order ID is required.");

        if (expiresAtUtc.HasValue && expiresAtUtc.Value <= DateTimeOffset.UtcNow)
            throw new DomainException("Expires-at must be in the future.");

        return new InventoryReservation(orderId, expiresAtUtc);
    }

    public void AddItem(Guid warehouseId, Guid productVariantId, int quantity)
    {
        EnsureActive();

        if (warehouseId == Guid.Empty)
            throw new DomainException("Warehouse ID is required.");

        if (productVariantId == Guid.Empty)
            throw new DomainException("Product variant ID is required.");

        if (quantity <= 0)
            throw new DomainException("Quantity must be positive.");

        if (_items.Any(i => i.WarehouseId == warehouseId && i.ProductVariantId == productVariantId))
            throw new DomainException("Reservation already contains this warehouse and variant.");

        _items.Add(new InventoryReservationItem(Id, warehouseId, productVariantId, quantity));
    }

    public void Consume()
    {
        EnsureActive();

        Status = InventoryReservationStatus.Consumed;
        Touch();
    }

    public void Release()
    {
        EnsureActive();

        Status = InventoryReservationStatus.Released;
        ReleasedAtUtc = DateTimeOffset.UtcNow;
        Touch();
    }

    public void Expire()
    {
        EnsureActive();

        Status = InventoryReservationStatus.Expired;
        Touch();
    }

    private void EnsureActive()
    {
        if (Status != InventoryReservationStatus.Active)
            throw new DomainException("Inventory reservation is not active.");
    }

    private void Touch() => UpdatedAtUtc = DateTimeOffset.UtcNow;
}
