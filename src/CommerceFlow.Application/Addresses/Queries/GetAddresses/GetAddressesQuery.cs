using CommerceFlow.Application.Catalog.Products.Queries;
using MediatR;

namespace CommerceFlow.Application.Addresses.Queries.GetAddresses;

public sealed record GetAddressesQuery(
    Guid? CustomerId = null,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PagedResult<AddressDto>>;
