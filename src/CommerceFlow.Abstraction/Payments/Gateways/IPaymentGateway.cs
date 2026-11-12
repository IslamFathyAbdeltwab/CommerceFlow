namespace CommerceFlow.Application.Payments.Gateways;

// Strategy: one implementation per payment provider (Stripe, PayPal, ...).
// The Application layer orchestrates; the provider-specific algorithm lives
// entirely behind this interface. Adding a provider = one new class plus one
// DI registration - no edits to existing code (open/closed).
public interface IPaymentGateway
{
    // Matches the provider name stored on Payment (compared case-insensitively).
    string Provider { get; }

    Task<GatewayChargeResult> ChargeAsync(
        GatewayChargeRequest request,
        CancellationToken cancellationToken = default);

    Task<GatewayRefundResult> RefundAsync(
        GatewayRefundRequest request,
        CancellationToken cancellationToken = default);
}
