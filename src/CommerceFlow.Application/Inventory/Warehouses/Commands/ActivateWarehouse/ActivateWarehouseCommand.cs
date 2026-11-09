using MediatR;

namespace CommerceFlow.Application.Inventory.Warehouses.Commands.ActivateWarehouse;

public sealed record ActivateWarehouseCommand(Guid WarehouseId) : IRequest;
