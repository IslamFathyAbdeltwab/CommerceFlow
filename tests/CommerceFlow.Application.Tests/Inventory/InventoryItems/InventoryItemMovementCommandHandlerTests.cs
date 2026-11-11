using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Inventory.Interfaces;
using CommerceFlow.Application.Inventory.InventoryItems.Commands.AdjustStock;
using CommerceFlow.Application.Inventory.InventoryItems.Commands.ReceiveStock;
using CommerceFlow.Application.Inventory.InventoryItems.Commands.ShipStock;
using CommerceFlow.Domain.Entities.Inventory;
using CommerceFlow.Domain.Enums;
using CommerceFlow.Domain.Exceptions;
using Moq;

namespace CommerceFlow.Application.Tests.Inventory.InventoryItems;

public sealed class InventoryItemMovementCommandHandlerTests
{
    private readonly Mock<IInventoryItemRepository> _items = new();
    private readonly Mock<IStockMovementRepository> _movements = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private static InventoryItem CreateItem(int onHand = 100) =>
        InventoryItem.Create(Guid.NewGuid(), Guid.NewGuid(), onHand);

    private void SetupItem(InventoryItem item) =>
        _items
            .Setup(x => x.GetByIdAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

    // ---------- Receive ----------

    [Fact]
    public async Task Receive_WhenItemDoesNotExist_ThrowsNotFoundAndRecordsNoMovement()
    {
        var handler = new ReceiveStockCommandHandler(_items.Object, _movements.Object, _unitOfWork.Object);
        var itemId = Guid.NewGuid();
        _items
            .Setup(x => x.GetByIdAsync(itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryItem?)null);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new ReceiveStockCommand(itemId, 10, "Delivery"), CancellationToken.None));

        Assert.Contains("InventoryItem", exception.Message);
        _movements.Verify(x => x.AddAsync(It.IsAny<StockMovement>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Receive_IncreasesOnHandAndRecordsStockReceivedMovement()
    {
        var handler = new ReceiveStockCommandHandler(_items.Object, _movements.Object, _unitOfWork.Object);
        var item = CreateItem(onHand: 100);
        SetupItem(item);

        await handler.Handle(new ReceiveStockCommand(item.Id, 25, "Delivery PO-1"), CancellationToken.None);

        Assert.Equal(125, item.OnHandQuantity);
        _movements.Verify(
            x => x.AddAsync(
                It.Is<StockMovement>(m => m.WarehouseId == item.WarehouseId
                    && m.ProductVariantId == item.ProductVariantId
                    && m.MovementType == StockMovementType.StockReceived
                    && m.QuantityChange == 25
                    && m.QuantityAfter == 125
                    && m.Reason == "Delivery PO-1"),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Receive_WithNonPositiveQuantity_ThrowsDomainException()
    {
        var handler = new ReceiveStockCommandHandler(_items.Object, _movements.Object, _unitOfWork.Object);
        var item = CreateItem();
        SetupItem(item);

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new ReceiveStockCommand(item.Id, 0, "Delivery"), CancellationToken.None));

        Assert.Equal(100, item.OnHandQuantity);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Receive_WithBlankReason_ThrowsDomainException()
    {
        var handler = new ReceiveStockCommandHandler(_items.Object, _movements.Object, _unitOfWork.Object);
        var item = CreateItem();
        SetupItem(item);

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new ReceiveStockCommand(item.Id, 5, "  "), CancellationToken.None));
    }

    // ---------- Ship ----------

    [Fact]
    public async Task Ship_WhenItemDoesNotExist_ThrowsNotFound()
    {
        var handler = new ShipStockCommandHandler(_items.Object, _movements.Object, _unitOfWork.Object);
        var itemId = Guid.NewGuid();
        _items
            .Setup(x => x.GetByIdAsync(itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryItem?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new ShipStockCommand(itemId, 10, "Order shipment"), CancellationToken.None));

        _movements.Verify(x => x.AddAsync(It.IsAny<StockMovement>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Ship_DecreasesOnHandAndRecordsShipmentMovementWithNegativeQuantity()
    {
        var handler = new ShipStockCommandHandler(_items.Object, _movements.Object, _unitOfWork.Object);
        var item = CreateItem(onHand: 100);
        SetupItem(item);

        await handler.Handle(new ShipStockCommand(item.Id, 30, "Order shipment"), CancellationToken.None);

        Assert.Equal(70, item.OnHandQuantity);
        _movements.Verify(
            x => x.AddAsync(
                It.Is<StockMovement>(m => m.MovementType == StockMovementType.Shipment
                    && m.QuantityChange == -30
                    && m.QuantityAfter == 70
                    && m.Reason == "Order shipment"),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Ship_MoreThanAvailable_ThrowsDomainExceptionAndKeepsQuantity()
    {
        var handler = new ShipStockCommandHandler(_items.Object, _movements.Object, _unitOfWork.Object);
        var item = CreateItem(onHand: 100);
        item.Reserve(95); // only 5 available
        SetupItem(item);

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new ShipStockCommand(item.Id, 10, "Order shipment"), CancellationToken.None));

        Assert.Equal(100, item.OnHandQuantity);
        _movements.Verify(x => x.AddAsync(It.IsAny<StockMovement>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---------- Adjust ----------

    [Fact]
    public async Task Adjust_WhenItemDoesNotExist_ThrowsNotFound()
    {
        var handler = new AdjustStockCommandHandler(_items.Object, _movements.Object, _unitOfWork.Object);
        var itemId = Guid.NewGuid();
        _items
            .Setup(x => x.GetByIdAsync(itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryItem?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new AdjustStockCommand(itemId, -5, "Cycle count"), CancellationToken.None));
    }

    [Fact]
    public async Task Adjust_WithNegativeChange_RecordsManualAdjustmentMovement()
    {
        var handler = new AdjustStockCommandHandler(_items.Object, _movements.Object, _unitOfWork.Object);
        var item = CreateItem(onHand: 100);
        SetupItem(item);

        await handler.Handle(new AdjustStockCommand(item.Id, -5, "Cycle count"), CancellationToken.None);

        Assert.Equal(95, item.OnHandQuantity);
        _movements.Verify(
            x => x.AddAsync(
                It.Is<StockMovement>(m => m.MovementType == StockMovementType.ManualAdjustment
                    && m.QuantityChange == -5
                    && m.QuantityAfter == 95
                    && m.Reason == "Cycle count"),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Adjust_WithPositiveChange_IncreasesOnHand()
    {
        var handler = new AdjustStockCommandHandler(_items.Object, _movements.Object, _unitOfWork.Object);
        var item = CreateItem(onHand: 100);
        SetupItem(item);

        await handler.Handle(new AdjustStockCommand(item.Id, 7, "Found stock"), CancellationToken.None);

        Assert.Equal(107, item.OnHandQuantity);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Adjust_WithZeroChange_ThrowsDomainException()
    {
        var handler = new AdjustStockCommandHandler(_items.Object, _movements.Object, _unitOfWork.Object);
        var item = CreateItem();
        SetupItem(item);

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new AdjustStockCommand(item.Id, 0, "No-op"), CancellationToken.None));

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Adjust_MakingAvailableNegative_ThrowsDomainException()
    {
        var handler = new AdjustStockCommandHandler(_items.Object, _movements.Object, _unitOfWork.Object);
        var item = CreateItem(onHand: 100);
        item.Reserve(90); // only 10 available
        SetupItem(item);

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new AdjustStockCommand(item.Id, -11, "Write-off"), CancellationToken.None));

        Assert.Equal(100, item.OnHandQuantity);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
