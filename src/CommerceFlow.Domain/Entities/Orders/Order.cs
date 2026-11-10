using CommerceFlow.Domain.Common;
using CommerceFlow.Domain.Enums;
using CommerceFlow.Domain.Exceptions;

namespace CommerceFlow.Domain.Entities.Orders;

// Aggregate root: a customer order. Owns its lines and the address snapshot.
// Fulfillment (OrderStatus) and payment (OrderPaymentStatus) are independent
// lifecycles; cross-aggregate references (customer, variant) are ID-only.
public class Order : AggregateRoot
{
    public const int CustomerNoteMaxLength = 1000;
    public const int CancelReasonMaxLength = 500;
    public const int MaxLines = 100;
    public const int CurrencyLength = 3;

    private readonly List<OrderLine> _lines = [];

    public string OrderNumber { get; private set; } = string.Empty;
    public Guid CustomerId { get; private set; }
    public OrderStatus Status { get; private set; }
    public OrderPaymentStatus PaymentStatus { get; private set; }
    public string Currency { get; private set; } = "USD";

    // Immutable snapshot of where the goods go (owned value object in EF).
    public AddressSnapshot ShippingAddress { get; private set; } = null!;

    public decimal Subtotal { get; private set; }
    public decimal ShippingFee { get; private set; }
    public decimal DiscountAmount { get; private set; }

    // Derived - not persisted (same pattern as InventoryItem.AvailableQuantity).
    public decimal GrandTotal => Subtotal + ShippingFee - DiscountAmount;

    public string? CustomerNote { get; private set; }
    public string? CancelReason { get; private set; }

    public DateTimeOffset? PaidAtUtc { get; private set; }
    public DateTimeOffset? ShippedAtUtc { get; private set; }
    public DateTimeOffset? DeliveredAtUtc { get; private set; }
    public DateTimeOffset? CancelledAtUtc { get; private set; }
    public DateTimeOffset? RefundedAtUtc { get; private set; }

    public IReadOnlyCollection<OrderLine> Lines => _lines.AsReadOnly();

    private Order() { } // required by EF Core

    private Order(Guid customerId, string currency, AddressSnapshot shippingAddress, string? customerNote)
    {
        OrderNumber = GenerateOrderNumber();
        CustomerId = customerId;
        Status = OrderStatus.Pending;
        PaymentStatus = OrderPaymentStatus.Unpaid;
        Currency = currency;
        ShippingAddress = shippingAddress;
        CustomerNote = customerNote;
        CreatedAtUtc = UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public static Order Create(
        Guid customerId,
        AddressSnapshot shippingAddress,
        string currency = "USD",
        string? customerNote = null)
    {
        if (customerId == Guid.Empty)
            throw new DomainException("Customer ID is required.");

        if (shippingAddress is null)
            throw new DomainException("Shipping address is required.");

        var normalizedCurrency = (currency ?? string.Empty).Trim().ToUpperInvariant();
        if (normalizedCurrency.Length != CurrencyLength || normalizedCurrency.Any(c => !char.IsLetter(c)))
            throw new DomainException("Currency must be a 3-letter ISO code.");

        var trimmedNote = string.IsNullOrWhiteSpace(customerNote) ? null : customerNote.Trim();
        if (trimmedNote is { Length: > CustomerNoteMaxLength })
            throw new DomainException($"Customer note cannot exceed {CustomerNoteMaxLength} characters.");

        return new Order(customerId, normalizedCurrency, shippingAddress, trimmedNote);
    }

    public static string GenerateOrderNumber()
    {
        var suffix = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        return $"ORD-{DateTime.UtcNow:yyyyMMdd}-{suffix}";
    }

    // ---------- Lines & totals (editable only while Pending) ----------

    public OrderLine AddLine(Guid productVariantId, string sku, string productName, decimal unitPrice, int quantity)
    {
        EnsureEditable();

        if (_lines.Count >= MaxLines)
            throw new DomainException($"An order cannot have more than {MaxLines} lines.");

        if (_lines.Any(l => l.ProductVariantId == productVariantId))
            throw new DomainException("Variant is already on this order; update its quantity instead.");

        var line = new OrderLine(Id, productVariantId, sku, productName, unitPrice, quantity);

        _lines.Add(line);
        Subtotal = _lines.Sum(l => l.LineTotal);
        Touch();
        return line;
    }

    public void RemoveLine(Guid lineId)
    {
        EnsureEditable();

        var line = FindLine(lineId);
        var newSubtotal = Subtotal - line.LineTotal;
        EnsureTotalsValid(newSubtotal, ShippingFee, DiscountAmount);

        _lines.Remove(line);
        Subtotal = newSubtotal;
        Touch();
    }

    public void UpdateLineQuantity(Guid lineId, int quantity)
    {
        EnsureEditable();
        OrderLine.EnsureValidQuantity(quantity);

        var line = FindLine(lineId);
        var newSubtotal = Subtotal - line.LineTotal + (line.UnitPrice * quantity);
        EnsureTotalsValid(newSubtotal, ShippingFee, DiscountAmount);

        line.UpdateQuantity(quantity);
        Subtotal = newSubtotal;
        Touch();
    }

    public void SetShippingFee(decimal shippingFee)
    {
        EnsureEditable();

        if (shippingFee < 0)
            throw new DomainException("Shipping fee cannot be negative.");

        EnsureTotalsValid(Subtotal, shippingFee, DiscountAmount);

        ShippingFee = shippingFee;
        Touch();
    }

    public void ApplyDiscount(decimal discountAmount)
    {
        EnsureEditable();

        if (discountAmount < 0)
            throw new DomainException("Discount cannot be negative.");

        EnsureTotalsValid(Subtotal, ShippingFee, discountAmount);

        DiscountAmount = discountAmount;
        Touch();
    }

    // ---------- Payment (independent lifecycle) ----------

    public void MarkAsPaid()
    {
        if (PaymentStatus != OrderPaymentStatus.Unpaid)
            throw new DomainException("Order payment is already recorded.");

        if (Status is not (OrderStatus.Pending or OrderStatus.Confirmed))
            throw new DomainException("Only pending or confirmed orders can be marked as paid.");

        PaymentStatus = OrderPaymentStatus.Paid;
        PaidAtUtc = DateTimeOffset.UtcNow;
        Touch();
    }

    public void Refund()
    {
        if (PaymentStatus != OrderPaymentStatus.Paid)
            throw new DomainException("Only paid orders can be refunded.");

        PaymentStatus = OrderPaymentStatus.Refunded;
        RefundedAtUtc = DateTimeOffset.UtcNow;
        Touch();
    }

    // ---------- Fulfillment state machine ----------

    public void Confirm()
    {
        EnsureStatus(OrderStatus.Pending, "confirm");

        if (_lines.Count == 0)
            throw new DomainException("Cannot confirm an order without lines.");

        Status = OrderStatus.Confirmed;
        Touch();
    }

    public void Ship()
    {
        EnsureStatus(OrderStatus.Confirmed, "ship");

        if (_lines.Count == 0)
            throw new DomainException("Cannot ship an order without lines.");

        Status = OrderStatus.Shipped;
        ShippedAtUtc = DateTimeOffset.UtcNow;
        Touch();
    }

    public void Deliver()
    {
        EnsureStatus(OrderStatus.Shipped, "deliver");

        Status = OrderStatus.Delivered;
        DeliveredAtUtc = DateTimeOffset.UtcNow;
        Touch();
    }

    public void Cancel(string? reason = null)
    {
        if (Status is not (OrderStatus.Pending or OrderStatus.Confirmed))
            throw new DomainException("Only pending or confirmed orders can be cancelled.");

        var trimmedReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        if (trimmedReason is { Length: > CancelReasonMaxLength })
            throw new DomainException($"Cancel reason cannot exceed {CancelReasonMaxLength} characters.");

        Status = OrderStatus.Cancelled;
        CancelReason = trimmedReason;
        CancelledAtUtc = DateTimeOffset.UtcNow;
        Touch();
    }

    // ---------- Helpers ----------

    private OrderLine FindLine(Guid lineId) =>
        _lines.FirstOrDefault(l => l.Id == lineId)
        ?? throw new DomainException("Order line not found.");

    private void EnsureEditable()
    {
        if (Status != OrderStatus.Pending)
            throw new DomainException("Lines and totals can only be changed while the order is pending.");
    }

    private void EnsureStatus(OrderStatus expected, string action)
    {
        if (Status != expected)
            throw new DomainException($"Cannot {action} an order in {Status} status.");
    }

    private static void EnsureTotalsValid(decimal subtotal, decimal shippingFee, decimal discountAmount)
    {
        if (subtotal + shippingFee - discountAmount < 0)
            throw new DomainException("Discount cannot exceed the order total.");
    }

    private void Touch() => UpdatedAtUtc = DateTimeOffset.UtcNow;
}
