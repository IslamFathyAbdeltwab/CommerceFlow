using CommerceFlow.Domain.Entities.Payments;
using CommerceFlow.Domain.Enums;
using CommerceFlow.Domain.Exceptions;

namespace CommerceFlow.Domain.Tests.Payments;

public class PaymentTests
{
    private static readonly Guid OrderId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();

    private static Payment NewPayment(decimal amount = 99.99m, string currency = "USD") =>
        Payment.Create(
            OrderId,
            CustomerId,
            amount,
            currency,
            provider: "Stripe",
            idempotencyKey: "idem-key-123");

    private static Payment SucceededPayment()
    {
        var payment = NewPayment();
        payment.MarkSucceeded("txn_123");
        return payment;
    }

    private static Payment FailedPayment()
    {
        var payment = NewPayment();
        payment.MarkFailed("Card declined");
        return payment;
    }

    private static Payment RefundedPayment()
    {
        var payment = SucceededPayment();
        payment.Refund();
        return payment;
    }

    // ---------- Create ----------

    [Fact]
    public void Create_ValidData_CreatesPendingPaymentForOrder()
    {
        var payment = NewPayment();

        Assert.Equal(OrderId, payment.OrderId);
        Assert.Equal(CustomerId, payment.CustomerId);
        Assert.Equal(99.99m, payment.Amount);
        Assert.Equal("USD", payment.Currency);
        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.Equal("Stripe", payment.Provider);
        Assert.Equal("idem-key-123", payment.IdempotencyKey);
        Assert.Null(payment.ProviderTransactionId);
        Assert.Null(payment.FailureReason);
        Assert.Null(payment.ProcessedAtUtc);
        Assert.Null(payment.RefundedAtUtc);
        Assert.NotEqual(Guid.Empty, payment.Id);
    }

    [Fact]
    public void Create_SetsAuditTimestamps()
    {
        var payment = NewPayment();

        Assert.NotEqual(default, payment.CreatedAtUtc);
        Assert.NotEqual(default, payment.UpdatedAtUtc);
        Assert.True(payment.UpdatedAtUtc >= payment.CreatedAtUtc);
    }

    [Fact]
    public void Create_EmptyOrderId_Throws()
    {
        var exception = Assert.Throws<DomainException>(() =>
            Payment.Create(
                Guid.Empty, CustomerId, 99.99m, "USD", "Stripe", "idem-key-123"));

        Assert.Equal("Order ID is required.", exception.Message);
    }

    [Fact]
    public void Create_EmptyCustomerId_Throws()
    {
        var exception = Assert.Throws<DomainException>(() =>
            Payment.Create(
                OrderId, Guid.Empty, 99.99m, "USD", "Stripe", "idem-key-123"));

        Assert.Equal("Customer ID is required.", exception.Message);
    }

    [Fact]
    public void Create_ZeroAmount_Throws()
    {
        var exception = Assert.Throws<DomainException>(() => NewPayment(amount: 0m));

        Assert.Equal("Payment amount must be greater than zero.", exception.Message);
    }

    [Fact]
    public void Create_NegativeAmount_Throws()
    {
        var exception = Assert.Throws<DomainException>(() => NewPayment(amount: -5m));

        Assert.Equal("Payment amount must be greater than zero.", exception.Message);
    }

    [Fact]
    public void Create_AmountWithSubCentPrecision_Throws()
    {
        var exception = Assert.Throws<DomainException>(() => NewPayment(amount: 10.005m));

        Assert.Equal(
            "Payment amount cannot have more than 2 decimal places.", exception.Message);
    }

    [Fact]
    public void Create_AmountWithUpToTwoDecimals_IsAccepted()
    {
        Assert.Equal(0.01m, NewPayment(amount: 0.01m).Amount);
        Assert.Equal(99.99m, NewPayment(amount: 99.99m).Amount);
        Assert.Equal(100.50m, NewPayment(amount: 100.50m).Amount);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("US")]
    [InlineData("DOLLARS")]
    [InlineData("D12")]
    public void Create_InvalidCurrency_Throws(string? currency)
    {
        var exception = Assert.Throws<DomainException>(() =>
            NewPayment(currency: currency!));

        Assert.Equal("Currency must be a 3-letter ISO code.", exception.Message);
    }

    [Fact]
    public void Create_CurrencyWithWhitespaceOrLowercase_IsNormalizedToUpper()
    {
        Assert.Equal("USD", NewPayment(currency: " usd ").Currency);
        Assert.Equal("EUR", NewPayment(currency: "eur").Currency);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_MissingProvider_Throws(string provider)
    {
        var exception = Assert.Throws<DomainException>(() =>
            Payment.Create(OrderId, CustomerId, 99.99m, "USD", provider, "idem-key-123"));

        Assert.Equal("Provider is required.", exception.Message);
    }

    [Fact]
    public void Create_ProviderTooLong_Throws()
    {
        var exception = Assert.Throws<DomainException>(() =>
            Payment.Create(
                OrderId, CustomerId, 99.99m, "USD",
                new string('x', Payment.ProviderMaxLength + 1), "idem-key-123"));

        Assert.Equal("Provider cannot exceed 50 characters.", exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_MissingIdempotencyKey_Throws(string idempotencyKey)
    {
        var exception = Assert.Throws<DomainException>(() =>
            Payment.Create(OrderId, CustomerId, 99.99m, "USD", "Stripe", idempotencyKey));

        Assert.Equal("Idempotency key is required.", exception.Message);
    }

    [Fact]
    public void Create_IdempotencyKeyTooLong_Throws()
    {
        var exception = Assert.Throws<DomainException>(() =>
            Payment.Create(
                OrderId, CustomerId, 99.99m, "USD", "Stripe",
                new string('k', Payment.IdempotencyKeyMaxLength + 1)));

        Assert.Equal("Idempotency key cannot exceed 100 characters.", exception.Message);
    }

    [Fact]
    public void Create_TrimsProviderAndIdempotencyKey()
    {
        var payment = Payment.Create(
            OrderId, CustomerId, 99.99m, "USD", "  Stripe  ", "  key-123  ");

        Assert.Equal("Stripe", payment.Provider);
        Assert.Equal("key-123", payment.IdempotencyKey);
    }

    // ---------- MarkSucceeded ----------

    [Fact]
    public void MarkSucceeded_FromPending_SucceedsAndRecordsGatewayReference()
    {
        var payment = NewPayment();

        payment.MarkSucceeded("  txn_123  ");

        Assert.Equal(PaymentStatus.Succeeded, payment.Status);
        Assert.Equal("txn_123", payment.ProviderTransactionId); // trimmed
        Assert.NotNull(payment.ProcessedAtUtc);
        Assert.True(payment.ProcessedAtUtc >= payment.CreatedAtUtc);
        Assert.Null(payment.RefundedAtUtc);
        Assert.Null(payment.FailureReason);
        Assert.True(payment.UpdatedAtUtc >= payment.CreatedAtUtc);
    }

    [Fact]
    public void MarkSucceeded_MissingTransactionId_ThrowsAndStaysPending()
    {
        var payment = NewPayment();

        var exception = Assert.Throws<DomainException>(() =>
            payment.MarkSucceeded("  "));

        Assert.Equal("Provider transaction ID is required.", exception.Message);
        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.Null(payment.ProcessedAtUtc);
    }

    [Fact]
    public void MarkSucceeded_TransactionIdTooLong_Throws()
    {
        var payment = NewPayment();

        var exception = Assert.Throws<DomainException>(() =>
            payment.MarkSucceeded(new string('t', Payment.ProviderTransactionIdMaxLength + 1)));

        Assert.Equal(
            "Provider transaction ID cannot exceed 100 characters.", exception.Message);
        Assert.Equal(PaymentStatus.Pending, payment.Status);
    }

    [Fact]
    public void MarkSucceeded_WhenAlreadySucceeded_IsNoOp()
    {
        // webhooks are delivered at least once: the duplicate must not fail
        // nor overwrite the original reference/timestamps
        var payment = SucceededPayment();
        var transactionId = payment.ProviderTransactionId;
        var processedAt = payment.ProcessedAtUtc;
        var updatedAt = payment.UpdatedAtUtc;

        payment.MarkSucceeded("txn_duplicate");

        Assert.Equal(PaymentStatus.Succeeded, payment.Status);
        Assert.Equal(transactionId, payment.ProviderTransactionId);
        Assert.Equal(processedAt, payment.ProcessedAtUtc);
        Assert.Equal(updatedAt, payment.UpdatedAtUtc); // no change -> no touch
    }

    [Fact]
    public void MarkSucceeded_WhenFailed_ThrowsAndKeepsFailure()
    {
        var payment = FailedPayment();

        var exception = Assert.Throws<DomainException>(() =>
            payment.MarkSucceeded("txn_456"));

        Assert.Equal("Only pending payments can succeed.", exception.Message);
        Assert.Equal(PaymentStatus.Failed, payment.Status);
        Assert.Equal("Card declined", payment.FailureReason);
        Assert.Null(payment.ProviderTransactionId);
    }

    [Fact]
    public void MarkSucceeded_WhenRefunded_Throws()
    {
        var payment = RefundedPayment();

        var exception = Assert.Throws<DomainException>(() =>
            payment.MarkSucceeded("txn_456"));

        Assert.Equal("Only pending payments can succeed.", exception.Message);
        Assert.Equal(PaymentStatus.Refunded, payment.Status);
    }

    // ---------- MarkFailed ----------

    [Fact]
    public void MarkFailed_FromPending_FailsAndStoresReason()
    {
        var payment = NewPayment();

        payment.MarkFailed("  Card declined  ");

        Assert.Equal(PaymentStatus.Failed, payment.Status);
        Assert.Equal("Card declined", payment.FailureReason); // trimmed
        Assert.NotNull(payment.ProcessedAtUtc);
        Assert.Null(payment.ProviderTransactionId);
        Assert.Null(payment.RefundedAtUtc);
    }

    [Fact]
    public void MarkFailed_MissingReason_ThrowsAndStaysPending()
    {
        var payment = NewPayment();

        var exception = Assert.Throws<DomainException>(() => payment.MarkFailed("   "));

        Assert.Equal("Failure reason is required.", exception.Message);
        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.Null(payment.ProcessedAtUtc);
    }

    [Fact]
    public void MarkFailed_ReasonTooLong_Throws()
    {
        var payment = NewPayment();

        var exception = Assert.Throws<DomainException>(() =>
            payment.MarkFailed(new string('r', Payment.FailureReasonMaxLength + 1)));

        Assert.Equal("Failure reason cannot exceed 500 characters.", exception.Message);
        Assert.Equal(PaymentStatus.Pending, payment.Status);
    }

    [Fact]
    public void MarkFailed_WhenAlreadyFailed_IsNoOp()
    {
        var payment = FailedPayment();
        var processedAt = payment.ProcessedAtUtc;
        var updatedAt = payment.UpdatedAtUtc;

        payment.MarkFailed("Insufficient funds");

        Assert.Equal(PaymentStatus.Failed, payment.Status);
        Assert.Equal("Card declined", payment.FailureReason); // original kept
        Assert.Equal(processedAt, payment.ProcessedAtUtc);
        Assert.Equal(updatedAt, payment.UpdatedAtUtc);
    }

    [Fact]
    public void MarkFailed_WhenSucceeded_ThrowsAndKeepsSuccess()
    {
        var payment = SucceededPayment();

        var exception = Assert.Throws<DomainException>(() =>
            payment.MarkFailed("Reversal notice"));

        Assert.Equal("Only pending payments can fail.", exception.Message);
        Assert.Equal(PaymentStatus.Succeeded, payment.Status);
        Assert.Equal("txn_123", payment.ProviderTransactionId);
        Assert.Null(payment.FailureReason);
    }

    [Fact]
    public void MarkFailed_WhenRefunded_Throws()
    {
        var payment = RefundedPayment();

        var exception = Assert.Throws<DomainException>(() =>
            payment.MarkFailed("Late failure"));

        Assert.Equal("Only pending payments can fail.", exception.Message);
        Assert.Equal(PaymentStatus.Refunded, payment.Status);
    }

    // ---------- Refund ----------

    [Fact]
    public void Refund_FromSucceeded_RefundsAndSetsTimestamp()
    {
        var payment = SucceededPayment();
        var processedAt = payment.ProcessedAtUtc;

        payment.Refund();

        Assert.Equal(PaymentStatus.Refunded, payment.Status);
        Assert.NotNull(payment.RefundedAtUtc);
        Assert.True(payment.RefundedAtUtc >= processedAt);
        Assert.Equal(processedAt, payment.ProcessedAtUtc); // still processed then
        Assert.Equal("txn_123", payment.ProviderTransactionId); // audit trail kept
    }

    [Fact]
    public void Refund_WhenAlreadyRefunded_IsNoOp()
    {
        var payment = RefundedPayment();
        var refundedAt = payment.RefundedAtUtc;
        var updatedAt = payment.UpdatedAtUtc;

        payment.Refund();

        Assert.Equal(PaymentStatus.Refunded, payment.Status);
        Assert.Equal(refundedAt, payment.RefundedAtUtc);
        Assert.Equal(updatedAt, payment.UpdatedAtUtc); // no change -> no touch
    }

    [Fact]
    public void Refund_WhenPending_ThrowsAndStaysPending()
    {
        var payment = NewPayment();

        var exception = Assert.Throws<DomainException>(() => payment.Refund());

        Assert.Equal("Only succeeded payments can be refunded.", exception.Message);
        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.Null(payment.RefundedAtUtc);
    }

    [Fact]
    public void Refund_WhenFailed_ThrowsAndKeepsFailure()
    {
        var payment = FailedPayment();

        var exception = Assert.Throws<DomainException>(() => payment.Refund());

        Assert.Equal("Only succeeded payments can be refunded.", exception.Message);
        Assert.Equal(PaymentStatus.Failed, payment.Status);
        Assert.Equal("Card declined", payment.FailureReason);
        Assert.Null(payment.RefundedAtUtc);
    }
}
