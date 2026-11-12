using CommerceFlow.Application.Payments.Gateways;

namespace CommerceFlow.Infrastructure.Payments.Gateways;

// Sandbox simulator of the PayPal strategy (see StripeGateway for the note
// on swapping in a real provider client).
internal sealed class PayPalGateway : IPaymentGateway
{
    public string Provider => "PayPal";

    public Task<GatewayChargeResult> ChargeAsync(
        GatewayChargeRequest request,
        CancellationToken cancellationToken = default)
        => Task.FromResult(
            GatewayChargeResult.Success($"paypal_{request.PaymentId:N}"));

    public Task<GatewayRefundResult> RefundAsync(
        GatewayRefundRequest request,
        CancellationToken cancellationToken = default)
        => Task.FromResult(GatewayRefundResult.Success());
}
