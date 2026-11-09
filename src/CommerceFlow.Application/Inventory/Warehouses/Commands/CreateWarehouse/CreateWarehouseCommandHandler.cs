using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Inventory.Interfaces;
using CommerceFlow.Domain.Entities.Inventory;
using MediatR;

namespace CommerceFlow.Application.Inventory.Warehouses.Commands.CreateWarehouse;

public sealed class CreateWarehouseCommandHandler(
    IWarehouseRepository warehouses,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateWarehouseCommand, Guid>
{
    public async Task<Guid> Handle(CreateWarehouseCommand request, CancellationToken cancellationToken)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        if (await warehouses.CodeExistsAsync(code, cancellationToken: cancellationToken))
            throw new ConflictException($"A warehouse with code '{code}' already exists.");

        var warehouse = Warehouse.Create(request.Code, request.Name, request.Description);

        await warehouses.AddAsync(warehouse, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return warehouse.Id;
    }
}
