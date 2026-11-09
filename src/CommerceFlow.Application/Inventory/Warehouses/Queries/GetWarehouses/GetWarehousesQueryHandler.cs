using CommerceFlow.Application.Catalog.Products.Queries;
using CommerceFlow.Application.Inventory.Interfaces;
using MediatR;

namespace CommerceFlow.Application.Inventory.Warehouses.Queries.GetWarehouses;

public sealed class GetWarehousesQueryHandler(
    IWarehouseRepository warehouses) : IRequestHandler<GetWarehousesQuery, PagedResult<WarehouseListItemDto>>
{
    public async Task<PagedResult<WarehouseListItemDto>> Handle(GetWarehousesQuery request, CancellationToken cancellationToken)
    {
        var totalCount = await warehouses.CountAsync(
            request.SearchTerm?.Trim(),
            request.IsActive,
            cancellationToken);

        var list = await warehouses.GetAllAsync(
            request.SearchTerm?.Trim(),
            request.IsActive,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var items = list.Select(w => new WarehouseListItemDto(w.Id, w.Code, w.Name, w.IsActive)).ToList();

        return new PagedResult<WarehouseListItemDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}
