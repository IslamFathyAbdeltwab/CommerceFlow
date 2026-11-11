using MediatR;

namespace CommerceFlow.Application.Carts.Commands.CheckoutCart;

// Marks the cart as converted. The actual Order creation is orchestrated
// separately (Orders module) and takes the cart's lines + a fresh address.
public sealed record CheckoutCartCommand(Guid CartId) : IRequest;
