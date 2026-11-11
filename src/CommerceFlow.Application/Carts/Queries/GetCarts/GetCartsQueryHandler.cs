using CommerceFlow.Application.Catalog.Products.Queries;
using CommerceFlow.Application.Carts.Interfaces;
using MediatR;

namespace CommerceFlow.Application.Carts.Queries.GetCarts;

public sealed class GetCartsQueryHandler(
    ICartRepository carts) : IRequestHandler<GetCartsQuery, PagedResult<CartListItemDto>>
{
    public async Task<PagedResult<CartListItemDto>> Handle(GetCartsQuery request, CancellationToken cancellationToken)
    {
        var totalCount = await carts.CountAsync(
            request.CustomerId,
            request.Status,
            cancellationToken);

        var list = await carts.GetAllAsync(
            request.CustomerId,
            request.Status,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var items = list.Select(CartMappings.ToListItem).ToList();

        return new PagedResult<CartListItemDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}
