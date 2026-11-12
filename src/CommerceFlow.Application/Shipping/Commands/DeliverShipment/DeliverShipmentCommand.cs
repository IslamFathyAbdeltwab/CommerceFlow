using MediatR;

namespace CommerceFlow.Application.Shipping.Commands.DeliverShipment;

// Carrier webhook: the recipient signed for the box. Syncs the order to
// Delivered through IOrderRepository when the order itself was handed over -
// the same guarded cross-module call ProcessPayment uses for payments.
public sealed record DeliverShipmentCommand(Guid ShipmentId) : IRequest;
