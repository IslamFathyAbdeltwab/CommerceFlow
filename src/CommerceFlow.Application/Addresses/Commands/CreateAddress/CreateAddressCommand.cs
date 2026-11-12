using MediatR;

namespace CommerceFlow.Application.Addresses.Commands.CreateAddress;

public sealed record CreateAddressCommand(
    Guid CustomerId,
    string FullName,
    string Line1,
    string City,
    string PostalCode,
    string Country,
    string? Line2 = null,
    string? State = null,
    string? PhoneNumber = null,
    bool IsDefault = false) : IRequest<Guid>;
