using CommerceFlow.Application.Payments.Gateways;

namespace CommerceFlow.Infrastructure.Payments.Gateways;

// Sandbox simulator of the Stripe strategy. A real implementation would call
// Stripe's Charges/Refunds API here and map the HTTP response onto the result
// factories; the rest of the system is unaffected either way (that is the
// point of the Strategy boundary).
internal sealed class StripeGateway : IPaymentGateway
{
    public string Provider => "Stripe";

    public Task<GatewayChargeResult> ChargeAsync(
        GatewayChargeRequest request,
        CancellationToken cancellationToken = default)
        => Task.FromResult(
            GatewayChargeResult.Success($"stripe_{request.PaymentId:N}"));

    public Task<GatewayRefundResult> RefundAsync(
        GatewayRefundRequest request,
        CancellationToken cancellationToken = default)
        => Task.FromResult(GatewayRefundResult.Success());
}
