using MediatR;

namespace CommerceFlow.Application.Shipping.Commands.CancelShipment;

// Stops a label that was never handed to the carrier (order changed its mind
// before pickup). The order itself is never touched by this call.
public sealed record CancelShipmentCommand(Guid ShipmentId) : IRequest;
