using CommerceFlow.Application.Inventory.Interfaces;
using MediatR;

namespace CommerceFlow.Application.Inventory.Warehouses.Queries.GetWarehouseById;

public sealed class GetWarehouseByIdQueryHandler(
    IWarehouseRepository warehouses) : IRequestHandler<GetWarehouseByIdQuery, WarehouseDto?>
{
    public async Task<WarehouseDto?> Handle(GetWarehouseByIdQuery request, CancellationToken cancellationToken)
    {
        var warehouse = await warehouses.GetByIdAsync(request.WarehouseId, cancellationToken);
        if (warehouse is null)
            return null;

        return new WarehouseDto(
            warehouse.Id,
            warehouse.Code,
            warehouse.Name,
            warehouse.Description,
            warehouse.IsActive,
            warehouse.CreatedAtUtc,
            warehouse.UpdatedAtUtc);
    }
}
