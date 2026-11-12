using MediatR;

namespace CommerceFlow.Application.Shipping.Commands.ReturnShipment;

// After a failed attempt the carrier sends the box back to the sender.
// Released from the order's unshipped quantity, so the remaining units can
// be shipped again in a new shipment.
public sealed record ReturnShipmentCommand(Guid ShipmentId) : IRequest;
