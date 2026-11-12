using CommerceFlow.Application.Addresses.Interfaces;
using MediatR;

namespace CommerceFlow.Application.Addresses.Queries.GetAddressById;

public sealed class GetAddressByIdQueryHandler(
    ICustomerAddressRepository addresses) : IRequestHandler<GetAddressByIdQuery, AddressDto?>
{
    public async Task<AddressDto?> Handle(GetAddressByIdQuery request, CancellationToken cancellationToken)
    {
        var address = await addresses.GetByIdAsync(request.AddressId, cancellationToken);
        if (address is null)
            return null;

        return AddressMappings.ToDto(address);
    }
}
