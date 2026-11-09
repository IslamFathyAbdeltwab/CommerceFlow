using CommerceFlow.Domain.Entities.Inventory;
using FluentValidation;

namespace CommerceFlow.Application.Inventory.Warehouses.Commands.UpdateWarehouse;

public sealed class UpdateWarehouseCommandValidator : AbstractValidator<UpdateWarehouseCommand>
{
    public UpdateWarehouseCommandValidator()
    {
        RuleFor(x => x.WarehouseId).NotEmpty();

        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(Warehouse.CodeMaxLength);

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(Warehouse.NameMaxLength);

        RuleFor(x => x.Description)
            .MaximumLength(Warehouse.DescriptionMaxLength)
            .When(x => x.Description is not null);
    }
}
