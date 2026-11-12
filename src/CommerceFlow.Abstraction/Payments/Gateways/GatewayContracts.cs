namespace CommerceFlow.Application.Payments.Gateways;

// Everything a gateway needs to run a charge. IdempotencyKey is forwarded so
// the provider can deduplicate on its side too (two-level idempotency).
public sealed record GatewayChargeRequest(
    Guid PaymentId,
    Guid CustomerId,
    decimal Amount,
    string Currency,
    string IdempotencyKey);

public sealed record GatewayRefundRequest(
    Guid PaymentId,
    string ProviderTransactionId,
    decimal Amount,
    string Currency);

// Results of a gateway call. A decline or a rejected refund is an expected
// business outcome (money simply did not move), so it travels as data, not
// as an exception. The factories enforce the contract: success always
// carries a transaction id, failure always carries a reason - handlers can
// never receive a half-filled result.
public sealed record GatewayChargeResult(bool Succeeded, string TransactionId, string FailureReason)
{
    public static GatewayChargeResult Success(string transactionId) =>
        new(true, Required(transactionId, nameof(transactionId)), string.Empty);

    public static GatewayChargeResult Failure(string reason) =>
        new(false, string.Empty, Required(reason, nameof(reason)));

    private static string Required(string value, string name) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException($"{name} is required.", name)
            : value.Trim();
}

public sealed record GatewayRefundResult(bool Succeeded, string FailureReason)
{
    public static GatewayRefundResult Success() => new(true, string.Empty);

    public static GatewayRefundResult Failure(string reason) =>
        new(false, Required(reason, nameof(reason)));

    private static string Required(string value, string name) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException($"{name} is required.", name)
            : value.Trim();
}
