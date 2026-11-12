using MediatR;

namespace CommerceFlow.Application.Shipping.Commands.FailShipment;

// Carrier webhook: a delivery attempt failed (bad address, nobody home).
// The reason is kept for support/audit; the order is untouched - money comes
// back through Payments, not through a failed delivery.
public sealed record FailShipmentCommand(Guid ShipmentId, string Reason) : IRequest;
