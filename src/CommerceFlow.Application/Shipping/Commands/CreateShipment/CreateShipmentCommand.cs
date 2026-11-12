using MediatR;

namespace CommerceFlow.Application.Shipping.Commands.CreateShipment;

// CustomerId is deliberately absent: the order is the single source of truth
// for who receives the package (same call as CreatePaymentCommand). Items are
// checked against the order lines and the order's other shipments in the
// handler, so a box can never carry more than what was ordered.
// Returns the shipment id.
public sealed record CreateShipmentCommand(
    Guid OrderId,
    string Carrier,
    string TrackingNumber,
    IReadOnlyList<CreateShipmentItemDto> Items,
    string? ServiceLevel = null) : IRequest<Guid>;

public sealed record CreateShipmentItemDto(
    Guid ProductVariantId,
    int Quantity);
