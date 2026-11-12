using CommerceFlow.Domain.Common;
using CommerceFlow.Domain.Enums;
using CommerceFlow.Domain.Exceptions;

namespace CommerceFlow.Domain.Entities.Payments;

// Aggregate root: one payment attempt against an order. IdempotencyKey makes
// create-on-retry safe (the Application layer returns the existing payment
// instead of charging twice), and the status machine only moves forward.
// Order/customer references are ID-only like everywhere else: keeping
// Order.PaymentStatus in sync is the Application layer's job, exactly as
// AddressDefaults coordinates the default-address swap.
public class Payment : AggregateRoot
{
    public const int ProviderMaxLength = 50;
    public const int ProviderTransactionIdMaxLength = 100;
    public const int IdempotencyKeyMaxLength = 100;
    public const int FailureReasonMaxLength = 500;
    public const int CurrencyLength = 3;

    public Guid OrderId { get; private set; }
    public Guid CustomerId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "USD";
    public PaymentStatus Status { get; private set; }
    public string Provider { get; private set; } = string.Empty;
    public string IdempotencyKey { get; private set; } = string.Empty;

    // Set when the gateway answers: a reference on success, a reason on failure.
    public string? ProviderTransactionId { get; private set; }
    public string? FailureReason { get; private set; }

    public DateTimeOffset? ProcessedAtUtc { get; private set; }
    public DateTimeOffset? RefundedAtUtc { get; private set; }

    private Payment() { } // required by EF Core

    private Payment(
        Guid orderId,
        Guid customerId,
        decimal amount,
        string currency,
        string provider,
        string idempotencyKey)
    {
        OrderId = orderId;
        CustomerId = customerId;
        Amount = amount;
        Currency = currency;
        Status = PaymentStatus.Pending;
        Provider = provider;
        IdempotencyKey = idempotencyKey;
        CreatedAtUtc = UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public static Payment Create(
        Guid orderId,
        Guid customerId,
        decimal amount,
        string currency,
        string provider,
        string idempotencyKey)
    {
        if (orderId == Guid.Empty)
            throw new DomainException("Order ID is required.");

        if (customerId == Guid.Empty)
            throw new DomainException("Customer ID is required.");

        if (amount <= 0)
            throw new DomainException("Payment amount must be greater than zero.");

        // cents are the atomic unit: 10.005 can never be captured
        if (decimal.Round(amount, 2) != amount)
            throw new DomainException("Payment amount cannot have more than 2 decimal places.");

        var normalizedCurrency = (currency ?? string.Empty).Trim().ToUpperInvariant();
        if (normalizedCurrency.Length != CurrencyLength || normalizedCurrency.Any(c => !char.IsLetter(c)))
            throw new DomainException("Currency must be a 3-letter ISO code.");

        return new Payment(
            orderId,
            customerId,
            amount,
            normalizedCurrency,
            Required(provider, "Provider", ProviderMaxLength),
            Required(idempotencyKey, "Idempotency key", IdempotencyKeyMaxLength));
    }

    // Gateway confirmed the charge. Webhooks arrive at least once, so a
    // duplicate success for an already-succeeded payment is a no-op; moving
    // out of any other state is a real conflict.
    public void MarkSucceeded(string providerTransactionId)
    {
        var transactionId = Required(
            providerTransactionId, "Provider transaction ID", ProviderTransactionIdMaxLength);

        if (Status == PaymentStatus.Succeeded)
            return; // duplicate callback: already there

        if (Status != PaymentStatus.Pending)
            throw new DomainException("Only pending payments can succeed.");

        ProviderTransactionId = transactionId;
        Status = PaymentStatus.Succeeded;
        ProcessedAtUtc = DateTimeOffset.UtcNow;
        Touch();
    }

    // Gateway rejected the charge. Also idempotent for redelivered callbacks.
    public void MarkFailed(string reason)
    {
        var trimmedReason = Required(reason, "Failure reason", FailureReasonMaxLength);

        if (Status == PaymentStatus.Failed)
            return; // duplicate callback: already there

        if (Status != PaymentStatus.Pending)
            throw new DomainException("Only pending payments can fail.");

        FailureReason = trimmedReason;
        Status = PaymentStatus.Failed;
        ProcessedAtUtc = DateTimeOffset.UtcNow;
        Touch();
    }

    // Full refund only: a partial refund would need a RefundedAmount running
    // total (YAGNI until the business asks for it). Idempotent too - refund
    // APIs get retried.
    public void Refund()
    {
        if (Status == PaymentStatus.Refunded)
            return; // duplicate refund call

        if (Status != PaymentStatus.Succeeded)
            throw new DomainException("Only succeeded payments can be refunded.");

        Status = PaymentStatus.Refunded;
        RefundedAtUtc = DateTimeOffset.UtcNow;
        Touch();
    }

    // ---------- Validation (mirrors CustomerAddress) ----------

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
