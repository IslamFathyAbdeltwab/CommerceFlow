using CommerceFlow.Application.Catalog.Products.Queries;
using MediatR;

namespace CommerceFlow.Application.Inventory.Warehouses.Queries.GetWarehouses;

public sealed record GetWarehousesQuery(
    string? SearchTerm = null,
    bool? IsActive = null,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PagedResult<WarehouseListItemDto>>;
