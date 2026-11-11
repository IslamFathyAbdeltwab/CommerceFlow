using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Inventory.Interfaces;
using CommerceFlow.Application.Inventory.Reservations.Commands.ConsumeInventoryReservation;
using CommerceFlow.Application.Inventory.Reservations.Commands.CreateInventoryReservation;
using CommerceFlow.Application.Inventory.Reservations.Commands.ExpireInventoryReservation;
using CommerceFlow.Application.Inventory.Reservations.Commands.ReleaseInventoryReservation;
using CommerceFlow.Domain.Entities.Inventory;
using CommerceFlow.Domain.Enums;
using Moq;

namespace CommerceFlow.Application.Tests.Inventory.Reservations;

public sealed class ReservationCommandHandlerTests
{
    private readonly Mock<IInventoryReservationRepository> _reservations = new();
    private readonly Mock<IInventoryItemRepository> _items = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private void SetupReservation(InventoryReservation reservation) =>
        _reservations
            .Setup(x => x.GetByIdAsync(reservation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

    private void SetupInventoryItem(Guid warehouseId, Guid variantId, InventoryItem item) =>
        _items
            .Setup(x => x.GetByWarehouseAndVariantAsync(warehouseId, variantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

    // ---------- Create ----------

    [Fact]
    public async Task Create_WithNullItems_ThrowsConflict()
    {
        var handler = new CreateInventoryReservationCommandHandler(
            _reservations.Object, _items.Object, _unitOfWork.Object);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new CreateInventoryReservationCommand(Guid.NewGuid(), null!), CancellationToken.None));

        Assert.Contains("at least one item", exception.Message);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_WithEmptyItems_ThrowsConflict()
    {
        var handler = new CreateInventoryReservationCommandHandler(
            _reservations.Object, _items.Object, _unitOfWork.Object);

        await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(
                new CreateInventoryReservationCommand(Guid.NewGuid(), new List<CreateInventoryReservationItemDto>()),
                CancellationToken.None));

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_WhenOrderAlreadyHasReservation_ThrowsConflict()
    {
        var handler = new CreateInventoryReservationCommandHandler(
            _reservations.Object, _items.Object, _unitOfWork.Object);
        var orderId = Guid.NewGuid();
        _reservations
            .Setup(x => x.GetByOrderIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(InventoryReservation.Create(orderId));

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(
                new CreateInventoryReservationCommand(
                    orderId,
                    new List<CreateInventoryReservationItemDto> { new(Guid.NewGuid(), Guid.NewGuid(), 1) }),
                CancellationToken.None));

        Assert.Contains("already exists", exception.Message);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_WhenInventoryItemMissing_ThrowsNotFound()
    {
        var handler = new CreateInventoryReservationCommandHandler(
            _reservations.Object, _items.Object, _unitOfWork.Object);
        var warehouseId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        _items
            .Setup(x => x.GetByWarehouseAndVariantAsync(warehouseId, variantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryItem?)null);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new CreateInventoryReservationCommand(
                    Guid.NewGuid(),
                    new List<CreateInventoryReservationItemDto> { new(warehouseId, variantId, 5) }),
                CancellationToken.None));

        Assert.Contains("InventoryItem", exception.Message);
        _reservations.Verify(x => x.AddAsync(It.IsAny<InventoryReservation>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_WhenQuantityExceedsAvailable_ThrowsConflict()
    {
        var handler = new CreateInventoryReservationCommandHandler(
            _reservations.Object, _items.Object, _unitOfWork.Object);
        var warehouseId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var inventoryItem = InventoryItem.Create(warehouseId, variantId, onHandQuantity: 10);
        SetupInventoryItem(warehouseId, variantId, inventoryItem);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(
                new CreateInventoryReservationCommand(
                    Guid.NewGuid(),
                    new List<CreateInventoryReservationItemDto> { new(warehouseId, variantId, 30) }),
                CancellationToken.None));

        Assert.Contains("Cannot reserve more than available quantity", exception.Message);
        Assert.Equal(0, inventoryItem.ReservedQuantity);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_HappyPath_ReservesStockAddsItemsAndSaves()
    {
        var handler = new CreateInventoryReservationCommandHandler(
            _reservations.Object, _items.Object, _unitOfWork.Object);
        var orderId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var expiresAt = DateTimeOffset.UtcNow.AddHours(2);
        var inventoryItem = InventoryItem.Create(warehouseId, variantId, onHandQuantity: 100);
        SetupInventoryItem(warehouseId, variantId, inventoryItem);

        var id = await handler.Handle(
            new CreateInventoryReservationCommand(
                orderId,
                new List<CreateInventoryReservationItemDto> { new(warehouseId, variantId, 30) },
                expiresAt),
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, id);
        Assert.Equal(30, inventoryItem.ReservedQuantity);
        Assert.Equal(70, inventoryItem.AvailableQuantity);
        _reservations.Verify(
            x => x.AddAsync(
                It.Is<InventoryReservation>(r => r.OrderId == orderId
                    && r.Status == InventoryReservationStatus.Active
                    && r.ExpiresAtUtc == expiresAt
                    && r.Items.Single().WarehouseId == warehouseId
                    && r.Items.Single().ProductVariantId == variantId
                    && r.Items.Single().Quantity == 30),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_WhenLaterItemMissing_SavesNothing()
    {
        var handler = new CreateInventoryReservationCommandHandler(
            _reservations.Object, _items.Object, _unitOfWork.Object);
        var firstWarehouse = Guid.NewGuid();
        var firstVariant = Guid.NewGuid();
        var missingWarehouse = Guid.NewGuid();
        var missingVariant = Guid.NewGuid();
        var firstItem = InventoryItem.Create(firstWarehouse, firstVariant, onHandQuantity: 100);
        SetupInventoryItem(firstWarehouse, firstVariant, firstItem);
        SetupInventoryItem(missingWarehouse, missingVariant, null!);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new CreateInventoryReservationCommand(
                    Guid.NewGuid(),
                    new List<CreateInventoryReservationItemDto>
                    {
                        new(firstWarehouse, firstVariant, 10),
                        new(missingWarehouse, missingVariant, 10),
                    }),
                CancellationToken.None));

        // The first item was mutated in memory but nothing reached the database.
        _reservations.Verify(x => x.AddAsync(It.IsAny<InventoryReservation>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---------- Release ----------

    [Fact]
    public async Task Release_WhenReservationDoesNotExist_ThrowsNotFound()
    {
        var handler = new ReleaseInventoryReservationCommandHandler(
            _reservations.Object, _items.Object, _unitOfWork.Object);
        var reservationId = Guid.NewGuid();
        _reservations
            .Setup(x => x.GetByIdAsync(reservationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryReservation?)null);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new ReleaseInventoryReservationCommand(reservationId), CancellationToken.None));

        Assert.Contains("InventoryReservation", exception.Message);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Release_WhenInventoryItemMissing_ThrowsNotFoundAndDoesNotSave()
    {
        var handler = new ReleaseInventoryReservationCommandHandler(
            _reservations.Object, _items.Object, _unitOfWork.Object);
        var warehouseId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var reservation = InventoryReservation.Create(Guid.NewGuid());
        reservation.AddItem(warehouseId, variantId, 30);
        SetupReservation(reservation);
        SetupInventoryItem(warehouseId, variantId, null!);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new ReleaseInventoryReservationCommand(reservation.Id), CancellationToken.None));

        Assert.Equal(InventoryReservationStatus.Active, reservation.Status);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Release_HappyPath_ReturnsStockAndMarksReservationReleased()
    {
        var handler = new ReleaseInventoryReservationCommandHandler(
            _reservations.Object, _items.Object, _unitOfWork.Object);
        var warehouseId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var reservation = InventoryReservation.Create(Guid.NewGuid());
        reservation.AddItem(warehouseId, variantId, 30);
        var inventoryItem = InventoryItem.Create(warehouseId, variantId, onHandQuantity: 100);
        inventoryItem.Reserve(30);
        SetupReservation(reservation);
        SetupInventoryItem(warehouseId, variantId, inventoryItem);

        await handler.Handle(new ReleaseInventoryReservationCommand(reservation.Id), CancellationToken.None);

        Assert.Equal(0, inventoryItem.ReservedQuantity);
        Assert.Equal(100, inventoryItem.OnHandQuantity);
        Assert.Equal(InventoryReservationStatus.Released, reservation.Status);
        Assert.NotNull(reservation.ReleasedAtUtc);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---------- Consume ----------

    [Fact]
    public async Task Consume_WhenReservationDoesNotExist_ThrowsNotFound()
    {
        var handler = new ConsumeInventoryReservationCommandHandler(
            _reservations.Object, _items.Object, _unitOfWork.Object);
        var reservationId = Guid.NewGuid();
        _reservations
            .Setup(x => x.GetByIdAsync(reservationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryReservation?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new ConsumeInventoryReservationCommand(reservationId), CancellationToken.None));

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Consume_HappyPath_ShipsStockAndMarksReservationConsumed()
    {
        var handler = new ConsumeInventoryReservationCommandHandler(
            _reservations.Object, _items.Object, _unitOfWork.Object);
        var warehouseId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var reservation = InventoryReservation.Create(Guid.NewGuid());
        reservation.AddItem(warehouseId, variantId, 30);
        var inventoryItem = InventoryItem.Create(warehouseId, variantId, onHandQuantity: 100);
        inventoryItem.Reserve(30);
        SetupReservation(reservation);
        SetupInventoryItem(warehouseId, variantId, inventoryItem);

        await handler.Handle(new ConsumeInventoryReservationCommand(reservation.Id), CancellationToken.None);

        Assert.Equal(0, inventoryItem.ReservedQuantity);
        Assert.Equal(70, inventoryItem.OnHandQuantity); // goods physically left the warehouse
        Assert.Equal(InventoryReservationStatus.Consumed, reservation.Status);
        Assert.Null(reservation.ReleasedAtUtc);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---------- Expire ----------

    [Fact]
    public async Task Expire_WhenReservationDoesNotExist_ThrowsNotFound()
    {
        var handler = new ExpireInventoryReservationCommandHandler(
            _reservations.Object, _items.Object, _unitOfWork.Object);
        var reservationId = Guid.NewGuid();
        _reservations
            .Setup(x => x.GetByIdAsync(reservationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryReservation?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new ExpireInventoryReservationCommand(reservationId), CancellationToken.None));

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Expire_HappyPath_ReturnsStockAndMarksReservationExpired()
    {
        var handler = new ExpireInventoryReservationCommandHandler(
            _reservations.Object, _items.Object, _unitOfWork.Object);
        var warehouseId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var reservation = InventoryReservation.Create(Guid.NewGuid());
        reservation.AddItem(warehouseId, variantId, 30);
        var inventoryItem = InventoryItem.Create(warehouseId, variantId, onHandQuantity: 100);
        inventoryItem.Reserve(30);
        SetupReservation(reservation);
        SetupInventoryItem(warehouseId, variantId, inventoryItem);

        await handler.Handle(new ExpireInventoryReservationCommand(reservation.Id), CancellationToken.None);

        Assert.Equal(0, inventoryItem.ReservedQuantity);
        Assert.Equal(100, inventoryItem.OnHandQuantity);
        Assert.Equal(InventoryReservationStatus.Expired, reservation.Status);
        Assert.Null(reservation.ReleasedAtUtc); // expiry is not a manual release
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
