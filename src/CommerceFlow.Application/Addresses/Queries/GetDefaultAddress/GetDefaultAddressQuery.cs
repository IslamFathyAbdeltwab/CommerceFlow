using MediatR;

namespace CommerceFlow.Application.Addresses.Queries.GetDefaultAddress;

// Returns null when the customer has no default address (e.g. right after
// deleting the default), so the client can fall back to any address.
public sealed record GetDefaultAddressQuery(Guid CustomerId) : IRequest<AddressDto?>;
