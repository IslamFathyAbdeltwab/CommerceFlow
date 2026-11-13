using CommerceFlow.Domain.Common;
using CommerceFlow.Domain.Enums;
using CommerceFlow.Domain.Exceptions;

namespace CommerceFlow.Domain.Entities.Returns;

// Aggregate root: a customer's request to send part of an order back (RMA).
// OrderId/CustomerId are ID-only - Order stays another aggregate and money
// comes back through Payments, exactly as Shipping keeps Order.Status in sync
// from the Application layer. Multiple requests per order are allowed (like
// partial shipments); the Application layer sums them so returned quantities
// can never exceed what was ordered. The status machine below is an explicit
// forward-only table; repeating the current state is a no-op.
public class ReturnRequest : AggregateRoot
{
    public const int NotesMaxLength = 500;
    public const int RejectionReasonMaxLength = 500;
    public const int MaxItems = 100;

    // Forward-only transition table: only these moves exist. Terminal
    // states (Rejected, Completed, Cancelled) allow nothing out - a customer
    // wanting to try again submits a fresh request.
    private static readonly IReadOnlyDictionary<ReturnStatus, ReturnStatus[]> AllowedTransitions =
        new Dictionary<ReturnStatus, ReturnStatus[]>
        {
            [ReturnStatus.Pending]   = [ReturnStatus.Approved, ReturnStatus.Rejected, ReturnStatus.Cancelled],
            [ReturnStatus.Approved]  = [ReturnStatus.Received, ReturnStatus.Cancelled],
            [ReturnStatus.Received]  = [ReturnStatus.Completed],
            [ReturnStatus.Rejected]  = [],
            [ReturnStatus.Completed] = [],
            [ReturnStatus.Cancelled] = [],
        };

    private readonly List<ReturnItem> _items = [];

    public Guid OrderId { get; private set; }
    public Guid CustomerId { get; private set; }
    public ReturnStatus Status { get; private set; }
    public ReturnReason Reason { get; private set; }

    // Customer's free-text explanation (optional - the enum carries the why).
    public string? Notes { get; private set; }

    // Set on rejection; the reason survives duplicate moderation calls
    // (same rule as Shipment.FailureReason).
    public string? RejectionReason { get; private set; }

    public DateTimeOffset? ApprovedAtUtc { get; private set; }
    public DateTimeOffset? RejectedAtUtc { get; private set; }
    public DateTimeOffset? ReceivedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public DateTimeOffset? CancelledAtUtc { get; private set; }

    public IReadOnlyCollection<ReturnItem> Items => _items.AsReadOnly();

    private ReturnRequest() { } // required by EF Core

    private ReturnRequest(
        Guid orderId,
        Guid customerId,
        ReturnReason reason,
        string? notes)
    {
        OrderId = orderId;
        CustomerId = customerId;
        Status = ReturnStatus.Pending;
        Reason = reason;
        Notes = notes;
        CreatedAtUtc = UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public static ReturnRequest Create(
        Guid orderId,
        Guid customerId,
        ReturnReason reason,
        string? notes = null)
    {
        if (orderId == Guid.Empty)
            throw new DomainException("Order ID is required.");

        if (customerId == Guid.Empty)
            throw new DomainException("Customer ID is required.");

        if (!Enum.IsDefined(reason))
            throw new DomainException("Return reason is required.");

        return new ReturnRequest(
            orderId,
            customerId,
            reason,
            Optional(notes, "Notes", NotesMaxLength));
    }

    // The basket of goods goes back once - no edits after the merchant has
    // approved (or after anything else moved the request). One line per
    // variant - quantities merge upstream.
    public void AddItem(Guid productVariantId, int quantity)
    {
        if (Status != ReturnStatus.Pending)
            throw new DomainException(
                "Return items can only be added while the request is pending.");

        if (productVariantId == Guid.Empty)
            throw new DomainException("Product variant ID is required.");

        if (quantity <= 0)
            throw new DomainException("Quantity must be positive.");

        if (_items.Any(i => i.ProductVariantId == productVariantId))
            throw new DomainException("Return request already contains this variant.");

        if (_items.Count >= MaxItems)
            throw new DomainException(
                $"Return request cannot contain more than {MaxItems} items.");

        _items.Add(new ReturnItem(Id, productVariantId, quantity));
    }

    // Merchant accepts the return - the customer may now send the goods back.
    public void Approve()
    {
        if (Status == ReturnStatus.Approved)
            return; // duplicate moderation call: already there

        MoveTo(ReturnStatus.Approved);
        ApprovedAtUtc = DateTimeOffset.UtcNow;
    }

    // Merchant declines the return (outside policy, past the window, ...).
    // The reason is kept for support/audit.
    public void Reject(string reason)
    {
        var trimmedReason = Required(reason, "Rejection reason", RejectionReasonMaxLength);

        if (Status == ReturnStatus.Rejected)
            return; // duplicate moderation call: original reason kept

        MoveTo(ReturnStatus.Rejected);
        RejectionReason = trimmedReason;
        RejectedAtUtc = DateTimeOffset.UtcNow;
    }

    // Warehouse scanned the goods back in. A box with nothing in it is never
    // received - state check first, then the empty-guard, so a rejected or
    // cancelled request reports the transition (same order as
    // Shipment.MarkShipped).
    public void MarkReceived()
    {
        if (Status == ReturnStatus.Received)
            return; // duplicate scan: already there

        EnsureCanMoveTo(ReturnStatus.Received);

        if (_items.Count == 0)
            throw new DomainException(
                "Return request must contain at least one item before it can be received.");

        Status = ReturnStatus.Received;
        ReceivedAtUtc = DateTimeOffset.UtcNow;
        Touch();
    }

    // Return fully processed. Refunding the money is a Payments concern and
    // happens through the Application layer, not from here.
    public void Complete()
    {
        if (Status == ReturnStatus.Completed)
            return; // duplicate call: already there

        MoveTo(ReturnStatus.Completed);
        CompletedAtUtc = DateTimeOffset.UtcNow;
    }

    // Withdrawn by the customer while still pending, or revoked by the
    // merchant after approving but before receiving.
    public void Cancel()
    {
        if (Status == ReturnStatus.Cancelled)
            return; // duplicate cancel call

        MoveTo(ReturnStatus.Cancelled);
        CancelledAtUtc = DateTimeOffset.UtcNow;
    }

    // ---------- Transition machine + validation ----------

    private void MoveTo(ReturnStatus target)
    {
        EnsureCanMoveTo(target);
        Status = target;
        Touch();
    }

    private void EnsureCanMoveTo(ReturnStatus target)
    {
        if (!AllowedTransitions[Status].Contains(target))
            throw new DomainException(
                $"Return request cannot move from {Status} to {target}.");
    }

    private void Touch() => UpdatedAtUtc = DateTimeOffset.UtcNow;

    private static string Required(string value, string field, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException($"{field} is required.");

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
            throw new DomainException($"{field} cannot exceed {maxLength} characters.");

        return trimmed;
    }

    // Optional text: null/whitespace stays null, anything else is trimmed
    // and length-checked (same rule as Shipment.Create's service level).
    private static string? Optional(string? value, string field, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
            throw new DomainException($"{field} cannot exceed {maxLength} characters.");

        return trimmed;
    }
}
