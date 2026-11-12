using CommerceFlow.Application.Payments.Gateways;

namespace CommerceFlow.Infrastructure.Payments.Gateways;

// Sandbox gateway: no network, fully deterministic, so demos and smoke tests
// behave the same every run. The "decline" hook below lets Postman exercise
// the declined-charge path (payment -> Failed, order untouched) on demand.
internal sealed class FakePayGateway : IPaymentGateway
{
    public string Provider => "FakePay";

    public Task<GatewayChargeResult> ChargeAsync(
        GatewayChargeRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.IdempotencyKey.Contains("decline", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(GatewayChargeResult.Failure(
                "FakePay declined the card (simulated decline)."));

        return Task.FromResult(
            GatewayChargeResult.Success($"fakepay_{request.PaymentId:N}"));
    }

    public Task<GatewayRefundResult> RefundAsync(
        GatewayRefundRequest request,
        CancellationToken cancellationToken = default)
        => Task.FromResult(GatewayRefundResult.Success());
}
