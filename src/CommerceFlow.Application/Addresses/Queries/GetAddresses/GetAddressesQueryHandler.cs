using CommerceFlow.Application.Addresses.Interfaces;
using CommerceFlow.Application.Catalog.Products.Queries;
using MediatR;

namespace CommerceFlow.Application.Addresses.Queries.GetAddresses;

public sealed class GetAddressesQueryHandler(
    ICustomerAddressRepository addresses) : IRequestHandler<GetAddressesQuery, PagedResult<AddressDto>>
{
    public async Task<PagedResult<AddressDto>> Handle(GetAddressesQuery request, CancellationToken cancellationToken)
    {
        var totalCount = await addresses.CountAsync(
            request.CustomerId,
            cancellationToken);

        var list = await addresses.GetAllAsync(
            request.CustomerId,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var items = list.Select(AddressMappings.ToDto).ToList();

        return new PagedResult<AddressDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}
