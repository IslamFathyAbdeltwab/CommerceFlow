using MediatR;

namespace CommerceFlow.Application.Shipping.Commands.ShipShipment;

// Hands the box to the carrier and syncs the order's fulfillment state to
// Shipped through IOrderRepository (same cross-module call as ProcessPayment
// syncing Order.PaymentStatus) - one SaveChanges for both aggregates.
public sealed record ShipShipmentCommand(Guid ShipmentId) : IRequest;
