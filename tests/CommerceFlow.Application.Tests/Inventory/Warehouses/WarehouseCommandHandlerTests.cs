using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Inventory.Interfaces;
using CommerceFlow.Application.Inventory.Warehouses.Commands.ActivateWarehouse;
using CommerceFlow.Application.Inventory.Warehouses.Commands.CreateWarehouse;
using CommerceFlow.Application.Inventory.Warehouses.Commands.DeactivateWarehouse;
using CommerceFlow.Application.Inventory.Warehouses.Commands.UpdateWarehouse;
using CommerceFlow.Domain.Entities.Inventory;
using Moq;

namespace CommerceFlow.Application.Tests.Inventory.Warehouses;

public sealed class WarehouseCommandHandlerTests
{
    private readonly Mock<IWarehouseRepository> _warehouses = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    // ---------- Create ----------

    [Fact]
    public async Task Create_WhenCodeIsFree_AddsWarehouseWithNormalizedCodeAndSaves()
    {
        var handler = new CreateWarehouseCommandHandler(_warehouses.Object, _unitOfWork.Object);
        _warehouses
            .Setup(x => x.CodeExistsAsync("WH-1", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var id = await handler.Handle(
            new CreateWarehouseCommand(" wh-1 ", "Main Warehouse", "Central hub"),
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, id);
        _warehouses.Verify(
            x => x.AddAsync(
                It.Is<Warehouse>(w => w.Code == "WH-1"
                    && w.Name == "Main Warehouse"
                    && w.Description == "Central hub"
                    && w.IsActive),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_WhenCodeAlreadyExists_ThrowsConflictAndDoesNotSave()
    {
        var handler = new CreateWarehouseCommandHandler(_warehouses.Object, _unitOfWork.Object);
        _warehouses
            .Setup(x => x.CodeExistsAsync("WH-1", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new CreateWarehouseCommand("wh-1", "Main"), CancellationToken.None));

        Assert.Contains("'WH-1'", exception.Message);
        _warehouses.Verify(x => x.AddAsync(It.IsAny<Warehouse>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---------- Update ----------

    [Fact]
    public async Task Update_WhenWarehouseDoesNotExist_ThrowsNotFound()
    {
        var handler = new UpdateWarehouseCommandHandler(_warehouses.Object, _unitOfWork.Object);
        var warehouseId = Guid.NewGuid();
        _warehouses
            .Setup(x => x.GetByIdAsync(warehouseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Warehouse?)null);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new UpdateWarehouseCommand(warehouseId, "WH-1", "Main", null), CancellationToken.None));

        Assert.Contains("Warehouse", exception.Message);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Update_WhenCodeOwnedByAnotherWarehouse_ThrowsConflict()
    {
        var handler = new UpdateWarehouseCommandHandler(_warehouses.Object, _unitOfWork.Object);
        var warehouseId = Guid.NewGuid();
        _warehouses
            .Setup(x => x.GetByIdAsync(warehouseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Warehouse.Create("WH-1", "Main"));
        _warehouses
            .Setup(x => x.CodeExistsAsync("WH-2", warehouseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new UpdateWarehouseCommand(warehouseId, "wh-2", "Second", null), CancellationToken.None));

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Update_WhenCodeIsOwn_UpdatesDetailsAndSaves()
    {
        var handler = new UpdateWarehouseCommandHandler(_warehouses.Object, _unitOfWork.Object);
        var warehouseId = Guid.NewGuid();
        var existing = Warehouse.Create("WH-1", "Old name");
        _warehouses
            .Setup(x => x.GetByIdAsync(warehouseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _warehouses
            .Setup(x => x.CodeExistsAsync("WH-1", warehouseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await handler.Handle(
            new UpdateWarehouseCommand(warehouseId, "wh-1", "New name", "Updated description"),
            CancellationToken.None);

        Assert.Equal("WH-1", existing.Code);
        Assert.Equal("New name", existing.Name);
        Assert.Equal("Updated description", existing.Description);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---------- Activate / Deactivate ----------

    [Fact]
    public async Task Activate_WhenWarehouseDoesNotExist_ThrowsNotFound()
    {
        var handler = new ActivateWarehouseCommandHandler(_warehouses.Object, _unitOfWork.Object);
        var warehouseId = Guid.NewGuid();
        _warehouses
            .Setup(x => x.GetByIdAsync(warehouseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Warehouse?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new ActivateWarehouseCommand(warehouseId), CancellationToken.None));

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Activate_WhenInactive_ActivatesAndSaves()
    {
        var handler = new ActivateWarehouseCommandHandler(_warehouses.Object, _unitOfWork.Object);
        var warehouse = Warehouse.Create("WH-1", "Main");
        warehouse.Deactivate();
        _warehouses
            .Setup(x => x.GetByIdAsync(warehouse.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(warehouse);

        await handler.Handle(new ActivateWarehouseCommand(warehouse.Id), CancellationToken.None);

        Assert.True(warehouse.IsActive);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Deactivate_WhenWarehouseDoesNotExist_ThrowsNotFound()
    {
        var handler = new DeactivateWarehouseCommandHandler(_warehouses.Object, _unitOfWork.Object);
        var warehouseId = Guid.NewGuid();
        _warehouses
            .Setup(x => x.GetByIdAsync(warehouseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Warehouse?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new DeactivateWarehouseCommand(warehouseId), CancellationToken.None));

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Deactivate_WhenActive_DeactivatesAndSaves()
    {
        var handler = new DeactivateWarehouseCommandHandler(_warehouses.Object, _unitOfWork.Object);
        var warehouse = Warehouse.Create("WH-1", "Main"); // new warehouses start active
        _warehouses
            .Setup(x => x.GetByIdAsync(warehouse.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(warehouse);

        await handler.Handle(new DeactivateWarehouseCommand(warehouse.Id), CancellationToken.None);

        Assert.False(warehouse.IsActive);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
