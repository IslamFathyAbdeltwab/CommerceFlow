using CommerceFlow.Application.Inventory.Warehouses.Commands.CreateWarehouse;
using CommerceFlow.Application.Inventory.Warehouses.Commands.UpdateWarehouse;
using CommerceFlow.Application.Inventory.Warehouses.Queries.GetWarehouses;
using CommerceFlow.Domain.Entities.Inventory;

namespace CommerceFlow.Application.Tests.Inventory.Warehouses;

public sealed class WarehouseValidatorTests
{
    [Fact]
    public void CreateWarehouse_WithValidInput_IsValid()
    {
        var result = new CreateWarehouseCommandValidator()
            .Validate(new CreateWarehouseCommand("WH-1", "Main", "Central hub"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void CreateWarehouse_WithEmptyCode_IsInvalid()
    {
        var result = new CreateWarehouseCommandValidator()
            .Validate(new CreateWarehouseCommand("", "Main"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateWarehouseCommand.Code));
    }

    [Fact]
    public void CreateWarehouse_WithCodeOverMaxLength_IsInvalid()
    {
        var result = new CreateWarehouseCommandValidator()
            .Validate(new CreateWarehouseCommand(new string('x', Warehouse.CodeMaxLength + 1), "Main"));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void CreateWarehouse_WithEmptyName_IsInvalid()
    {
        var result = new CreateWarehouseCommandValidator()
            .Validate(new CreateWarehouseCommand("WH-1", ""));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateWarehouseCommand.Name));
    }

    [Fact]
    public void CreateWarehouse_WithDescriptionOverMaxLength_IsInvalid()
    {
        var result = new CreateWarehouseCommandValidator()
            .Validate(new CreateWarehouseCommand(
                "WH-1", "Main", new string('x', Warehouse.DescriptionMaxLength + 1)));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void UpdateWarehouse_WithEmptyWarehouseId_IsInvalid()
    {
        var result = new UpdateWarehouseCommandValidator()
            .Validate(new UpdateWarehouseCommand(Guid.Empty, "WH-1", "Main", null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateWarehouseCommand.WarehouseId));
    }

    [Fact]
    public void UpdateWarehouse_WithValidInput_IsValid()
    {
        var result = new UpdateWarehouseCommandValidator()
            .Validate(new UpdateWarehouseCommand(Guid.NewGuid(), "WH-1", "Main", null));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void GetWarehouses_WithDefaultPaging_IsValid()
    {
        var result = new GetWarehousesQueryValidator().Validate(new GetWarehousesQuery());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void GetWarehouses_WithOutOfRangePaging_IsInvalid()
    {
        var result = new GetWarehousesQueryValidator()
            .Validate(new GetWarehousesQuery(PageNumber: 0, PageSize: 101));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetWarehousesQuery.PageNumber));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetWarehousesQuery.PageSize));
    }

    [Fact]
    public void GetWarehouses_WithTooLongSearchTerm_IsInvalid()
    {
        var result = new GetWarehousesQueryValidator()
            .Validate(new GetWarehousesQuery(SearchTerm: new string('x', 201)));

        Assert.False(result.IsValid);
    }
}
