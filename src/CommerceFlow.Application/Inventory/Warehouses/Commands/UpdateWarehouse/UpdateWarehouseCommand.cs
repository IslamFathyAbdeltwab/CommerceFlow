using MediatR;

namespace CommerceFlow.Application.Inventory.Warehouses.Commands.UpdateWarehouse;

public sealed record UpdateWarehouseCommand(
    Guid WarehouseId,
    string Code,
    string Name,
    string? Description) : IRequest;
