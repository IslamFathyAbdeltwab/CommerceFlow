using MediatR;

namespace CommerceFlow.Application.Addresses.Commands.SetDefaultAddress;

public sealed record SetDefaultAddressCommand(Guid AddressId) : IRequest;
