using MediatR;

namespace CommerceFlow.Application.Carts.Queries.GetActiveCartByCustomerId;

// Returns null when the customer has no active cart, so the client can
// distinguish "no cart yet" from "cart not found".
public sealed record GetActiveCartByCustomerIdQuery(Guid CustomerId) : IRequest<CartDto?>;
