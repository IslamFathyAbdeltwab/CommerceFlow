using CommerceFlow.Application.Catalog.Products.Queries;
using MediatR;

namespace CommerceFlow.Application.Wishlists.Queries.GetWishlists;

public sealed record GetWishlistsQuery(
    Guid? CustomerId = null,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PagedResult<WishlistListItemDto>>;
