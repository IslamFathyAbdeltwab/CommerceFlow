using MediatR;

namespace CommerceFlow.Application.Shipping.Queries.GetShipmentById;

// null = not found (the controller maps it to 404, same as GetPaymentById).
public sealed record GetShipmentByIdQuery(Guid ShipmentId) : IRequest<ShipmentDto?>;
