using MediatR;

namespace CommerceFlow.Application.Carts.Commands.CheckoutCart;

// Converts an active cart into an Order and returns the new order id. The
// catalog is read at this moment (cart lines carry no money by design), the
// order is built from that snapshot and the cart is marked converted - all
// in ONE unit of work, so a committed checkout produces both or neither and
// a retry can never double-order.
public sealed record CheckoutCartCommand(
    Guid CartId,
    CheckoutAddressDto ShippingAddress,
    string? CustomerNote = null) : IRequest<Guid>;

// Same shape as PlaceOrderAddressDto: each command owns its input contract,
// so the two flows can drift apart without breaking each other.
public sealed record CheckoutAddressDto(
    string FullName,
    string Line1,
    string City,
    string PostalCode,
    string Country,
    string? Line2 = null,
    string? State = null,
    string? PhoneNumber = null);
