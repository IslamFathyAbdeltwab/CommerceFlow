using CommerceFlow.Domain.Entities.Inventory;
using FluentValidation;

namespace CommerceFlow.Application.Inventory.Warehouses.Commands.CreateWarehouse;

public sealed class CreateWarehouseCommandValidator : AbstractValidator<CreateWarehouseCommand>
{
    public CreateWarehouseCommandValidator()
    {
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
