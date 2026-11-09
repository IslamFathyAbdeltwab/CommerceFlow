using MediatR;

namespace CommerceFlow.Application.Inventory.Warehouses.Queries.GetWarehouseById;

public sealed record GetWarehouseByIdQuery(Guid WarehouseId) : IRequest<WarehouseDto?>;
