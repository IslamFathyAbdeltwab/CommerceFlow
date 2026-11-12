using CommerceFlow.Application.Payments.Gateways;

namespace CommerceFlow.Infrastructure.Payments.Gateways;

// Factory/registry over the Strategy implementations: every registered
// IPaymentGateway is indexed by its provider name (case-insensitive, matching
// the domain's comparison rule). Unknown -> null so handlers can answer 409
// BEFORE any money moves. Adding a provider stays one class + one DI line;
// a duplicate Provider fails fast at startup via ToDictionary.
internal sealed class PaymentGatewayResolver(IEnumerable<IPaymentGateway> gateways)
    : IPaymentGatewayResolver
{
    private readonly IReadOnlyDictionary<string, IPaymentGateway> _byProvider =
        gateways.ToDictionary(g => g.Provider, StringComparer.OrdinalIgnoreCase);

    public IPaymentGateway? Resolve(string provider)
    {
        if (string.IsNullOrWhiteSpace(provider))
            return null;

        return _byProvider.TryGetValue(provider, out var gateway) ? gateway : null;
    }
}
