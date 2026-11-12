namespace CommerceFlow.Application.Payments.Gateways;

// Factory that picks the right Strategy by provider name. Returns null for
// unknown providers so handlers can pre-check (409) before any money moves.
public interface IPaymentGatewayResolver
{
    IPaymentGateway? Resolve(string provider);
}
