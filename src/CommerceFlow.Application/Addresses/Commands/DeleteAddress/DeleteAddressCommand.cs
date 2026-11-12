using MediatR;

namespace CommerceFlow.Application.Addresses.Commands.DeleteAddress;

public sealed record DeleteAddressCommand(Guid AddressId) : IRequest;
