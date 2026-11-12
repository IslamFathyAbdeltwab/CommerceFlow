using MediatR;

namespace CommerceFlow.Application.Addresses.Queries.GetAddressById;

public sealed record GetAddressByIdQuery(Guid AddressId) : IRequest<AddressDto?>;
