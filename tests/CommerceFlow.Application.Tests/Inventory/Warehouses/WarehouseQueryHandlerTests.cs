using CommerceFlow.Application.Inventory.Interfaces;
using CommerceFlow.Application.Inventory.Warehouses.Queries.GetWarehouseById;
using CommerceFlow.Application.Inventory.Warehouses.Queries.GetWarehouses;
using CommerceFlow.Domain.Entities.Inventory;
using Moq;

namespace CommerceFlow.Application.Tests.Inventory.Warehouses;

public sealed class WarehouseQueryHandlerTests
{
    private readonly Mock<IWarehouseRepository> _warehouses = new();

    [Fact]
    public async Task GetWarehouseById_WhenWarehouseDoesNotExist_ReturnsNull()
    {
        var handler = new GetWarehouseByIdQueryHandler(_warehouses.Object);
        _warehouses
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Warehouse?)null);

        var result = await handler.Handle(new GetWarehouseByIdQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetWarehouseById_WhenWarehouseExists_MapsAllFields()
    {
        var handler = new GetWarehouseByIdQueryHandler(_warehouses.Object);
        var warehouse = Warehouse.Create("WH-1", "Main", "Central hub");
        warehouse.Deactivate();
        _warehouses
            .Setup(x => x.GetByIdAsync(warehouse.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(warehouse);

        var result = await handler.Handle(new GetWarehouseByIdQuery(warehouse.Id), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(warehouse.Id, result.Id);
        Assert.Equal("WH-1", result.Code);
        Assert.Equal("Main", result.Name);
        Assert.Equal("Central hub", result.Description);
        Assert.False(result.IsActive);
        Assert.Equal(warehouse.CreatedAtUtc, result.CreatedAtUtc);
        Assert.Equal(warehouse.UpdatedAtUtc, result.UpdatedAtUtc);
    }

    [Fact]
    public async Task GetWarehouses_ReturnsPagedResultWithMappedItems()
    {
        var handler = new GetWarehousesQueryHandler(_warehouses.Object);
        var first = Warehouse.Create("WH-1", "Main");
        var second = Warehouse.Create("WH-2", "Overflow");
        second.Deactivate();
        _warehouses
            .Setup(x => x.CountAsync("wh", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(30);
        _warehouses
            .Setup(x => x.GetAllAsync("wh", false, 2, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Warehouse> { first, second });

        var result = await handler.Handle(
            new GetWarehousesQuery(" wh ", false, PageNumber: 2, PageSize: 10),
            CancellationToken.None);

        Assert.Equal(30, result.TotalCount);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(3, result.TotalPages);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal("WH-1", result.Items[0].Code);
        Assert.True(result.Items[0].IsActive);
        Assert.False(result.Items[1].IsActive);
    }
}
