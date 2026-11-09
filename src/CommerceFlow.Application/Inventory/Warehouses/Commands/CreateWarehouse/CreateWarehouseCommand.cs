using MediatR;

namespace CommerceFlow.Application.Inventory.Warehouses.Commands.CreateWarehouse;

public sealed record CreateWarehouseCommand(
    string Code,
    string Name,
    string? Description = null) : IRequest<Guid>;
