using MediatR;

namespace CommerceFlow.Application.Inventory.Warehouses.Commands.DeactivateWarehouse;

public sealed record DeactivateWarehouseCommand(Guid WarehouseId) : IRequest;
