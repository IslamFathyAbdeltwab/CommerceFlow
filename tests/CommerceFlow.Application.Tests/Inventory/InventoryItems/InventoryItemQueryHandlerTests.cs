using CommerceFlow.Application.Inventory.Interfaces;
using CommerceFlow.Application.Inventory.InventoryItems.Queries.GetInventoryItemById;
using CommerceFlow.Application.Inventory.InventoryItems.Queries.GetInventoryItems;
using CommerceFlow.Domain.Entities.Inventory;
using Moq;

namespace CommerceFlow.Application.Tests.Inventory.InventoryItems;

public sealed class InventoryItemQueryHandlerTests
{
    private readonly Mock<IInventoryItemRepository> _items = new();

    [Fact]
    public async Task GetInventoryItemById_WhenItemDoesNotExist_ReturnsNull()
    {
        var handler = new GetInventoryItemByIdQueryHandler(_items.Object);
        _items
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryItem?)null);

        var result = await handler.Handle(new GetInventoryItemByIdQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetInventoryItemById_WhenItemExists_MapsDerivedQuantities()
    {
        var handler = new GetInventoryItemByIdQueryHandler(_items.Object);
        var item = InventoryItem.Create(Guid.NewGuid(), Guid.NewGuid(), 100, 20);
        item.Reserve(30);
        _items
            .Setup(x => x.GetByIdAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var result = await handler.Handle(new GetInventoryItemByIdQuery(item.Id), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(item.Id, result.Id);
        Assert.Equal(item.WarehouseId, result.WarehouseId);
        Assert.Equal(item.ProductVariantId, result.ProductVariantId);
        Assert.Equal(100, result.OnHandQuantity);
        Assert.Equal(30, result.ReservedQuantity);
        Assert.Equal(70, result.AvailableQuantity);
        Assert.Equal(20, result.ReorderThreshold);
        Assert.False(result.IsBelowReorderThreshold); // 70 > 20
        Assert.Equal(item.CreatedAtUtc, result.CreatedAtUtc);
        Assert.Equal(item.UpdatedAtUtc, result.UpdatedAtUtc);
    }

    [Fact]
    public async Task GetInventoryItems_ReturnsPagedResultWithLowStockFlag()
    {
        var handler = new GetInventoryItemsQueryHandler(_items.Object);
        var warehouseId = Guid.NewGuid();
        var healthy = InventoryItem.Create(warehouseId, Guid.NewGuid(), 100, 10);
        var low = InventoryItem.Create(warehouseId, Guid.NewGuid(), 8, 10); // 8 <= 10
        low.Reserve(3); // available 5, still below threshold
        _items
            .Setup(x => x.CountAsync(warehouseId, null, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _items
            .Setup(x => x.GetAllAsync(warehouseId, null, true, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<InventoryItem> { healthy, low });

        var result = await handler.Handle(
            new GetInventoryItemsQuery(WarehouseId: warehouseId, LowStockOnly: true),
            CancellationToken.None);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(100, result.Items[0].AvailableQuantity);
        Assert.False(result.Items[0].IsBelowReorderThreshold);
        Assert.Equal(5, result.Items[1].AvailableQuantity);
        Assert.True(result.Items[1].IsBelowReorderThreshold);
        _items.Verify(
            x => x.CountAsync(warehouseId, null, true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetInventoryItems_WithNoFilters_PassesNullsThrough()
    {
        var handler = new GetInventoryItemsQueryHandler(_items.Object);
        _items
            .Setup(x => x.CountAsync(null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _items
            .Setup(x => x.GetAllAsync(null, null, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<InventoryItem>());

        var result = await handler.Handle(new GetInventoryItemsQuery(), CancellationToken.None);

        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }
}
