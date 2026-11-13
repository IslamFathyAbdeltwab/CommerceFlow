using CommerceFlow.Application.Catalog.Products.Queries;
using CommerceFlow.Application.Returns.Interfaces;
using MediatR;

namespace CommerceFlow.Application.Returns.Queries.GetReturns;

public sealed class GetReturnsQueryHandler(IReturnRequestRepository returns)
    : IRequestHandler<GetReturnsQuery, PagedResult<ReturnRequestDto>>
{
    public async Task<PagedResult<ReturnRequestDto>> Handle(GetReturnsQuery request, CancellationToken cancellationToken)
    {
        var totalCount = await returns.CountAsync(
            request.OrderId,
            request.CustomerId,
            request.Status,
            cancellationToken);

        var list = await returns.GetAllAsync(
            request.OrderId,
            request.CustomerId,
            request.Status,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var items = list.Select(ReturnRequestMappings.ToDto).ToList();

        return new PagedResult<ReturnRequestDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}
