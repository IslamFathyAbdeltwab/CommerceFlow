using CommerceFlow.Application.Catalog.Products.Queries;
using CommerceFlow.Domain.Enums;
using MediatR;

namespace CommerceFlow.Application.Returns.Queries.GetReturns;

public sealed record GetReturnsQuery(
    Guid? OrderId = null,
    Guid? CustomerId = null,
    ReturnStatus? Status = null,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PagedResult<ReturnRequestDto>>;
