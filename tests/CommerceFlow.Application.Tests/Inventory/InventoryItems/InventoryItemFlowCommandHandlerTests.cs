using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Inventory.Interfaces;
using CommerceFlow.Application.Inventory.InventoryItems.Commands.ConsumeReservation;
using CommerceFlow.Application.Inventory.InventoryItems.Commands.CreateInventoryItem;
using CommerceFlow.Application.Inventory.InventoryItems.Commands.ReleaseReservation;
using CommerceFlow.Application.Inventory.InventoryItems.Commands.ReserveStock;
using CommerceFlow.Application.Inventory.InventoryItems.Commands.SetReorderThreshold;
using CommerceFlow.Domain.Entities.Inventory;
using CommerceFlow.Domain.Exceptions;
using Moq;

namespace CommerceFlow.Application.Tests.Inventory.InventoryItems;

public sealed class InventoryItemFlowCommandHandlerTests
{
    private readonly Mock<IInventoryItemRepository> _items = new();
    private readonly Mock<IWarehouseRepository> _warehouses = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private static InventoryItem CreateItem(int onHand = 100, int reorderThreshold = 10) =>
        InventoryItem.Create(Guid.NewGuid(), Guid.NewGuid(), onHand, reorderThreshold);

    private void SetupItem(InventoryItem item) =>
        _items
            .Setup(x => x.GetByIdAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

    // ---------- Create ----------

    [Fact]
    public async Task Create_WhenWarehouseDoesNotExist_ThrowsNotFound()
    {
        var handler = new CreateInventoryItemCommandHandler(_items.Object, _warehouses.Object, _unitOfWork.Object);
        var warehouseId = Guid.NewGuid();
        _warehouses
            .Setup(x => x.ExistsAsync(warehouseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new CreateInventoryItemCommand(warehouseId, Guid.NewGuid(), 50), CancellationToken.None));

        Assert.Contains("Warehouse", exception.Message);
        _items.Verify(x => x.AddAsync(It.IsAny<InventoryItem>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_WhenPairAlreadyExists_ThrowsConflict()
    {
        var handler = new CreateInventoryItemCommandHandler(_items.Object, _warehouses.Object, _unitOfWork.Object);
        var warehouseId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        _warehouses
            .Setup(x => x.ExistsAsync(warehouseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _items
            .Setup(x => x.ExistsAsync(warehouseId, variantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new CreateInventoryItemCommand(warehouseId, variantId, 50), CancellationToken.None));

        Assert.Contains("already exists", exception.Message);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_WhenPairIsFree_AddsItemAndSaves()
    {
        var handler = new CreateInventoryItemCommandHandler(_items.Object, _warehouses.Object, _unitOfWork.Object);
        var warehouseId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        _warehouses
            .Setup(x => x.ExistsAsync(warehouseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _items
            .Setup(x => x.ExistsAsync(warehouseId, variantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var id = await handler.Handle(
            new CreateInventoryItemCommand(warehouseId, variantId, OnHandQuantity: 50, ReorderThreshold: 5),
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, id);
        _items.Verify(
            x => x.AddAsync(
                It.Is<InventoryItem>(i => i.WarehouseId == warehouseId
                    && i.ProductVariantId == variantId
                    && i.OnHandQuantity == 50
                    && i.ReservedQuantity == 0
                    && i.ReorderThreshold == 5),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---------- Reserve ----------

    [Fact]
    public async Task Reserve_WhenItemDoesNotExist_ThrowsNotFound()
    {
        var handler = new ReserveStockCommandHandler(_items.Object, _unitOfWork.Object);
        var itemId = Guid.NewGuid();
        _items
            .Setup(x => x.GetByIdAsync(itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryItem?)null);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new ReserveStockCommand(itemId, 5), CancellationToken.None));

        Assert.Contains("InventoryItem", exception.Message);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Reserve_WhenEnoughAvailable_ReservesAndSaves()
    {
        var handler = new ReserveStockCommandHandler(_items.Object, _unitOfWork.Object);
        var item = CreateItem(onHand: 100);
        SetupItem(item);

        await handler.Handle(new ReserveStockCommand(item.Id, 30), CancellationToken.None);

        Assert.Equal(100, item.OnHandQuantity);
        Assert.Equal(30, item.ReservedQuantity);
        Assert.Equal(70, item.AvailableQuantity);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Reserve_MoreThanAvailable_ThrowsDomainException()
    {
        var handler = new ReserveStockCommandHandler(_items.Object, _unitOfWork.Object);
        var item = CreateItem(onHand: 100);
        SetupItem(item);

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new ReserveStockCommand(item.Id, 101), CancellationToken.None));

        Assert.Equal(0, item.ReservedQuantity);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---------- Release ----------

    [Fact]
    public async Task Release_WhenItemDoesNotExist_ThrowsNotFound()
    {
        var handler = new ReleaseReservationCommandHandler(_items.Object, _unitOfWork.Object);
        var itemId = Guid.NewGuid();
        _items
            .Setup(x => x.GetByIdAsync(itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryItem?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new ReleaseReservationCommand(itemId, 5), CancellationToken.None));
    }

    [Fact]
    public async Task Release_WhenEnoughReserved_ReleasesAndSaves()
    {
        var handler = new ReleaseReservationCommandHandler(_items.Object, _unitOfWork.Object);
        var item = CreateItem(onHand: 100);
        item.Reserve(30);
        SetupItem(item);

        await handler.Handle(new ReleaseReservationCommand(item.Id, 20), CancellationToken.None);

        Assert.Equal(10, item.ReservedQuantity);
        Assert.Equal(100, item.OnHandQuantity);
        Assert.Equal(90, item.AvailableQuantity);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Release_MoreThanReserved_ThrowsDomainException()
    {
        var handler = new ReleaseReservationCommandHandler(_items.Object, _unitOfWork.Object);
        var item = CreateItem(onHand: 100);
        item.Reserve(10);
        SetupItem(item);

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new ReleaseReservationCommand(item.Id, 11), CancellationToken.None));

        Assert.Equal(10, item.ReservedQuantity);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---------- Consume ----------

    [Fact]
    public async Task Consume_WhenItemDoesNotExist_ThrowsNotFound()
    {
        var handler = new ConsumeReservationCommandHandler(_items.Object, _unitOfWork.Object);
        var itemId = Guid.NewGuid();
        _items
            .Setup(x => x.GetByIdAsync(itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryItem?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new ConsumeReservationCommand(itemId, 5), CancellationToken.None));
    }

    [Fact]
    public async Task Consume_WhenEnoughReserved_ConsumesFromReservedAndOnHand()
    {
        var handler = new ConsumeReservationCommandHandler(_items.Object, _unitOfWork.Object);
        var item = CreateItem(onHand: 100);
        item.Reserve(30);
        SetupItem(item);

        await handler.Handle(new ConsumeReservationCommand(item.Id, 30), CancellationToken.None);

        Assert.Equal(0, item.ReservedQuantity);
        Assert.Equal(70, item.OnHandQuantity); // goods physically left the warehouse
        Assert.Equal(70, item.AvailableQuantity);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Consume_MoreThanReserved_ThrowsDomainException()
    {
        var handler = new ConsumeReservationCommandHandler(_items.Object, _unitOfWork.Object);
        var item = CreateItem(onHand: 100);
        item.Reserve(5);
        SetupItem(item);

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new ConsumeReservationCommand(item.Id, 6), CancellationToken.None));

        Assert.Equal(100, item.OnHandQuantity);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---------- Set reorder threshold ----------

    [Fact]
    public async Task SetReorderThreshold_WhenItemDoesNotExist_ThrowsNotFound()
    {
        var handler = new SetReorderThresholdCommandHandler(_items.Object, _unitOfWork.Object);
        var itemId = Guid.NewGuid();
        _items
            .Setup(x => x.GetByIdAsync(itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryItem?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new SetReorderThresholdCommand(itemId, 25), CancellationToken.None));
    }

    [Fact]
    public async Task SetReorderThreshold_WhenValid_UpdatesThresholdAndSaves()
    {
        var handler = new SetReorderThresholdCommandHandler(_items.Object, _unitOfWork.Object);
        var item = CreateItem(onHand: 100, reorderThreshold: 10);
        SetupItem(item);

        await handler.Handle(new SetReorderThresholdCommand(item.Id, 25), CancellationToken.None);

        Assert.Equal(25, item.ReorderThreshold);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetReorderThreshold_WhenNegative_ThrowsDomainException()
    {
        var handler = new SetReorderThresholdCommandHandler(_items.Object, _unitOfWork.Object);
        var item = CreateItem();
        SetupItem(item);

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new SetReorderThresholdCommand(item.Id, -1), CancellationToken.None));

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
