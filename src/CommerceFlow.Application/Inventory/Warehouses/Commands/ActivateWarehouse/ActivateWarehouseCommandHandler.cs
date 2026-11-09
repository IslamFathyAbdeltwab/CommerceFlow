using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Inventory.Interfaces;
using CommerceFlow.Domain.Entities.Inventory;
using MediatR;

namespace CommerceFlow.Application.Inventory.Warehouses.Commands.ActivateWarehouse;

public sealed class ActivateWarehouseCommandHandler(
    IWarehouseRepository warehouses,
    IUnitOfWork unitOfWork) : IRequestHandler<ActivateWarehouseCommand>
{
    public async Task Handle(ActivateWarehouseCommand request, CancellationToken cancellationToken)
    {
        var warehouse = await warehouses.GetByIdAsync(request.WarehouseId, cancellationToken)
            ?? throw new NotFoundException(nameof(Warehouse), request.WarehouseId);

        warehouse.Activate();

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
