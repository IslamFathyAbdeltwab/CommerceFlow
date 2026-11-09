using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Inventory.Interfaces;
using CommerceFlow.Domain.Entities.Inventory;
using MediatR;

namespace CommerceFlow.Application.Inventory.Warehouses.Commands.UpdateWarehouse;

public sealed class UpdateWarehouseCommandHandler(
    IWarehouseRepository warehouses,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateWarehouseCommand>
{
    public async Task Handle(UpdateWarehouseCommand request, CancellationToken cancellationToken)
    {
        var warehouse = await warehouses.GetByIdAsync(request.WarehouseId, cancellationToken)
            ?? throw new NotFoundException(nameof(Warehouse), request.WarehouseId);

        var code = request.Code.Trim().ToUpperInvariant();
        if (await warehouses.CodeExistsAsync(code, excludeWarehouseId: request.WarehouseId, cancellationToken: cancellationToken))
            throw new ConflictException($"A warehouse with code '{code}' already exists.");

        warehouse.UpdateDetails(request.Code, request.Name, request.Description);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
