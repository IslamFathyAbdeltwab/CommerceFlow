using MediatR;

namespace CommerceFlow.Application.Shipping.Commands.MarkShipmentInTransit;

// Carrier webhook: the package left the origin hub. The order is untouched -
// fulfillment only advances on Ship/Deliver, never mid-transit.
public sealed record MarkShipmentInTransitCommand(Guid ShipmentId) : IRequest;
