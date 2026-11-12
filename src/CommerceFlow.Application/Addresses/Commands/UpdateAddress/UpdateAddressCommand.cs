using MediatR;

namespace CommerceFlow.Application.Addresses.Commands.UpdateAddress;

public sealed record UpdateAddressCommand(
    Guid AddressId,
    string FullName,
    string Line1,
    string City,
    string PostalCode,
    string Country,
    string? Line2 = null,
    string? State = null,
    string? PhoneNumber = null) : IRequest;
