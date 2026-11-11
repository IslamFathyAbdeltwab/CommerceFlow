using CommerceFlow.Application.Catalog.Products.Queries;
using CommerceFlow.Application.Wishlists.Interfaces;
using MediatR;

namespace CommerceFlow.Application.Wishlists.Queries.GetWishlists;

public sealed class GetWishlistsQueryHandler(
    IWishlistRepository wishlists) : IRequestHandler<GetWishlistsQuery, PagedResult<WishlistListItemDto>>
{
    public async Task<PagedResult<WishlistListItemDto>> Handle(GetWishlistsQuery request, CancellationToken cancellationToken)
    {
        var totalCount = await wishlists.CountAsync(
            request.CustomerId,
            cancellationToken);

        var list = await wishlists.GetAllAsync(
            request.CustomerId,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var items = list.Select(WishlistMappings.ToListItem).ToList();

        return new PagedResult<WishlistListItemDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}
