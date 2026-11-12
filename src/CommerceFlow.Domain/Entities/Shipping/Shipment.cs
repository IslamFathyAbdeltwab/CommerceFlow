using CommerceFlow.Domain.Common;
using CommerceFlow.Domain.Enums;
using CommerceFlow.Domain.Exceptions;

namespace CommerceFlow.Domain.Entities.Shipping;

// Aggregate root: the carrier's record for (part of) an order. OrderId is
// ID-only - Order stays another aggregate and its OrderStatus moves in the
// Application layer, exactly as Payment keeps Order.PaymentStatus in sync.
// The status machine is an explicit forward-only table below; carrier
// webhooks arrive at least once, so repeating the current state is a no-op
// and anything outside the table is rejected.
public class Shipment : AggregateRoot
{
    public const int CarrierMaxLength = 100;
    public const int TrackingNumberMaxLength = 100;
    public const int ServiceLevelMaxLength = 50;
    public const int FailureReasonMaxLength = 500;
    public const int MaxItems = 100;

    // Forward-only transition table: only these moves exist. Terminal
    // states (Delivered, Returned, Cancelled) allow nothing out.
    private static readonly IReadOnlyDictionary<ShipmentStatus, ShipmentStatus[]> AllowedTransitions =
        new Dictionary<ShipmentStatus, ShipmentStatus[]>
        {
            [ShipmentStatus.Pending]   = [ShipmentStatus.Shipped, ShipmentStatus.Cancelled],
            [ShipmentStatus.Shipped]   = [ShipmentStatus.InTransit, ShipmentStatus.Delivered, ShipmentStatus.Failed],
            [ShipmentStatus.InTransit] = [ShipmentStatus.Delivered, ShipmentStatus.Failed],
            [ShipmentStatus.Failed]    = [ShipmentStatus.Returned],
            [ShipmentStatus.Delivered] = [],
            [ShipmentStatus.Returned]  = [],
            [ShipmentStatus.Cancelled] = [],
        };

    private readonly List<ShipmentItem> _items = [];

    public Guid OrderId { get; private set; }
    public ShipmentStatus Status { get; private set; }
    public string Carrier { get; private set; } = string.Empty;
    public string TrackingNumber { get; private set; } = string.Empty;
    public string? ServiceLevel { get; private set; }

    public DateTimeOffset? ShippedAtUtc { get; private set; }
    public DateTimeOffset? DeliveredAtUtc { get; private set; }
    public DateTimeOffset? CancelledAtUtc { get; private set; }

    // Set on a failed delivery attempt; the original reason survives
    // duplicate webhooks (same rule as Payment.FailureReason).
    public string? FailureReason { get; private set; }

    public IReadOnlyCollection<ShipmentItem> Items => _items.AsReadOnly();

    private Shipment() { } // required by EF Core

    private Shipment(
        Guid orderId,
        string carrier,
        string trackingNumber,
        string? serviceLevel)
    {
        OrderId = orderId;
        Status = ShipmentStatus.Pending;
        Carrier = carrier;
        TrackingNumber = trackingNumber;
        ServiceLevel = serviceLevel;
        CreatedAtUtc = UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public static Shipment Create(
        Guid orderId,
        string carrier,
        string trackingNumber,
        string? serviceLevel = null)
    {
        if (orderId == Guid.Empty)
            throw new DomainException("Order ID is required.");

        string? normalizedServiceLevel = null;
        if (serviceLevel is not null)
        {
            var trimmedServiceLevel = serviceLevel.Trim();
            if (trimmedServiceLevel.Length > ServiceLevelMaxLength)
                throw new DomainException(
                    $"Service level cannot exceed {ServiceLevelMaxLength} characters.");

            if (trimmedServiceLevel.Length > 0)
                normalizedServiceLevel = trimmedServiceLevel;
        }

        return new Shipment(
            orderId,
            Required(carrier, "Carrier", CarrierMaxLength),
            Required(trackingNumber, "Tracking number", TrackingNumberMaxLength),
            normalizedServiceLevel);
    }

    // Box contents are fixed once the carrier has the package: no edits
    // after hand-off. One line per variant - quantities merge upstream.
    public void AddItem(Guid productVariantId, int quantity)
    {
        if (Status != ShipmentStatus.Pending)
            throw new DomainException(
                "Shipment items can only be added while the shipment is pending.");

        if (productVariantId == Guid.Empty)
            throw new DomainException("Product variant ID is required.");

        if (quantity <= 0)
            throw new DomainException("Quantity must be positive.");

        if (_items.Any(i => i.ProductVariantId == productVariantId))
            throw new DomainException("Shipment already contains this variant.");

        if (_items.Count >= MaxItems)
            throw new DomainException(
                $"Shipment cannot contain more than {MaxItems} items.");

        _items.Add(new ShipmentItem(Id, productVariantId, quantity));
    }

    // Handed to the carrier. A box with nothing in it is never shipped.
    public void MarkShipped()
    {
        if (Status == ShipmentStatus.Shipped)
            return; // duplicate carrier webhook: already there

        EnsureCanMoveTo(ShipmentStatus.Shipped);

        if (_items.Count == 0)
            throw new DomainException(
                "Shipment must contain at least one item before it can be shipped.");

        Status = ShipmentStatus.Shipped;
        ShippedAtUtc = DateTimeOffset.UtcNow;
        Touch();
    }

    public void MarkInTransit()
    {
        if (Status == ShipmentStatus.InTransit)
            return; // duplicate carrier webhook: already there

        MoveTo(ShipmentStatus.InTransit);
    }

    // Some carriers scan Delivered without an intermediate InTransit scan,
    // so Shipped -> Delivered is a legal shortcut in the table.
    public void MarkDelivered()
    {
        if (Status == ShipmentStatus.Delivered)
            return; // duplicate carrier webhook: already there

        MoveTo(ShipmentStatus.Delivered);
        DeliveredAtUtc = DateTimeOffset.UtcNow;
    }

    // Carrier could not deliver (bad address, nobody home). Webhooks are
    // retried: a duplicate failure keeps the original reason untouched.
    public void MarkFailed(string reason)
    {
        var trimmedReason = Required(reason, "Failure reason", FailureReasonMaxLength);

        if (Status == ShipmentStatus.Failed)
            return; // duplicate carrier webhook: original reason kept

        MoveTo(ShipmentStatus.Failed);
        FailureReason = trimmedReason;
    }

    // After a failed attempt the carrier sends the package back.
    public void ReturnToSender()
    {
        if (Status == ShipmentStatus.Returned)
            return; // duplicate carrier webhook: already there

        MoveTo(ShipmentStatus.Returned);
    }

    // Stopped before hand-off - only a pending shipment can be cancelled;
    // once the carrier owns it, the only way back is a return.
    public void Cancel()
    {
        if (Status == ShipmentStatus.Cancelled)
            return; // duplicate cancel call

        MoveTo(ShipmentStatus.Cancelled);
        CancelledAtUtc = DateTimeOffset.UtcNow;
    }

    // ---------- Transition machine + validation ----------

    private void EnsureCanMoveTo(ShipmentStatus target)
    {
        if (!AllowedTransitions[Status].Contains(target))
            throw new DomainException(
                $"Shipment cannot move from {Status} to {target}.");
    }

    private void MoveTo(ShipmentStatus target)
    {
        EnsureCanMoveTo(target);

        Status = target;
        Touch();
    }

    // ---------- Validation (mirrors Payment) ----------

    private static string Required(string value, string field, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException($"{field} is required.");

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
            throw new DomainException($"{field} cannot exceed {maxLength} characters.");

        return trimmed;
    }

    private void Touch() => UpdatedAtUtc = DateTimeOffset.UtcNow;
}
