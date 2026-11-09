using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Inventory.Interfaces;
using CommerceFlow.Domain.Entities.Inventory;
using MediatR;

namespace CommerceFlow.Application.Inventory.Warehouses.Commands.DeactivateWarehouse;

public sealed class DeactivateWarehouseCommandHandler(
    IWarehouseRepository warehouses,
    IUnitOfWork unitOfWork) : IRequestHandler<DeactivateWarehouseCommand>
{
    public async Task Handle(DeactivateWarehouseCommand request, CancellationToken cancellationToken)
    {
        var warehouse = await warehouses.GetByIdAsync(request.WarehouseId, cancellationToken)
            ?? throw new NotFoundException(nameof(Warehouse), request.WarehouseId);

        warehouse.Deactivate();

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
