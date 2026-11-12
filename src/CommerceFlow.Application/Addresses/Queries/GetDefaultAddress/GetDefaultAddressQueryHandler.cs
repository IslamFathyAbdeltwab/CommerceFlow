using CommerceFlow.Application.Addresses.Interfaces;
using MediatR;

namespace CommerceFlow.Application.Addresses.Queries.GetDefaultAddress;

public sealed class GetDefaultAddressQueryHandler(
    ICustomerAddressRepository addresses) : IRequestHandler<GetDefaultAddressQuery, AddressDto?>
{
    public async Task<AddressDto?> Handle(GetDefaultAddressQuery request, CancellationToken cancellationToken)
    {
        var address = await addresses.GetDefaultAsync(request.CustomerId, cancellationToken);
        if (address is null)
            return null;

        return AddressMappings.ToDto(address);
    }
}
