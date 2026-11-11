using CommerceFlow.Application.Catalog.Products.Queries;
using CommerceFlow.Domain.Enums;
using MediatR;

namespace CommerceFlow.Application.Carts.Queries.GetCarts;

public sealed record GetCartsQuery(
    Guid? CustomerId = null,
    CartStatus? Status = null,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PagedResult<CartListItemDto>>;
