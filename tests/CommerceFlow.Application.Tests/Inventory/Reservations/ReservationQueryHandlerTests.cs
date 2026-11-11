using CommerceFlow.Application.Inventory.Interfaces;
using CommerceFlow.Application.Inventory.Reservations.Queries.GetInventoryReservationById;
using CommerceFlow.Application.Inventory.Reservations.Queries.GetInventoryReservations;
using CommerceFlow.Domain.Entities.Inventory;
using CommerceFlow.Domain.Enums;
using Moq;

namespace CommerceFlow.Application.Tests.Inventory.Reservations;

public sealed class ReservationQueryHandlerTests
{
    private readonly Mock<IInventoryReservationRepository> _reservations = new();

    [Fact]
    public async Task GetInventoryReservationById_WhenReservationDoesNotExist_ReturnsNull()
    {
        var handler = new GetInventoryReservationByIdQueryHandler(_reservations.Object);
        _reservations
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryReservation?)null);

        var result = await handler.Handle(new GetInventoryReservationByIdQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetInventoryReservationById_WhenReservationExists_MapsItems()
    {
        var handler = new GetInventoryReservationByIdQueryHandler(_reservations.Object);
        var warehouseId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var expiresAt = DateTimeOffset.UtcNow.AddHours(1);
        var reservation = InventoryReservation.Create(Guid.NewGuid(), expiresAt);
        reservation.AddItem(warehouseId, variantId, 30);
        _reservations
            .Setup(x => x.GetByIdAsync(reservation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        var result = await handler.Handle(new GetInventoryReservationByIdQuery(reservation.Id), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(reservation.Id, result.Id);
        Assert.Equal(reservation.OrderId, result.OrderId);
        Assert.Equal(InventoryReservationStatus.Active, result.Status);
        Assert.Equal(reservation.CreatedAtUtc, result.CreatedAtUtc);
        Assert.Equal(expiresAt, result.ExpiresAtUtc);
        Assert.Null(result.ReleasedAtUtc);

        var item = Assert.Single(result.Items);
        Assert.Equal(warehouseId, item.WarehouseId);
        Assert.Equal(variantId, item.ProductVariantId);
        Assert.Equal(30, item.Quantity);
        Assert.NotEqual(Guid.Empty, item.Id);
    }

    [Fact]
    public async Task GetInventoryReservations_ReturnsPagedResultWithFiltersPassedThrough()
    {
        var handler = new GetInventoryReservationsQueryHandler(_reservations.Object);
        var orderId = Guid.NewGuid();
        var reservation = InventoryReservation.Create(orderId);
        _reservations
            .Setup(x => x.CountAsync(orderId, InventoryReservationStatus.Active, It.IsAny<CancellationToken>()))
            .ReturnsAsync(7);
        _reservations
            .Setup(x => x.GetAllAsync(orderId, InventoryReservationStatus.Active, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<InventoryReservation> { reservation });

        var result = await handler.Handle(
            new GetInventoryReservationsQuery(OrderId: orderId, Status: InventoryReservationStatus.Active),
            CancellationToken.None);

        Assert.Equal(7, result.TotalCount);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(20, result.PageSize);
        var item = Assert.Single(result.Items);
        Assert.Equal(reservation.Id, item.Id);
        Assert.Equal(orderId, item.OrderId);
        Assert.Equal(InventoryReservationStatus.Active, item.Status);
        Assert.Equal(reservation.CreatedAtUtc, item.CreatedAtUtc);
        Assert.Null(item.ExpiresAtUtc);
        _reservations.Verify(
            x => x.CountAsync(orderId, InventoryReservationStatus.Active, It.IsAny<CancellationToken>()),
            Times.Once);
        _reservations.Verify(
            x => x.GetAllAsync(orderId, InventoryReservationStatus.Active, 1, 20, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetInventoryReservations_WithNoFilters_PassesNullsThrough()
    {
        var handler = new GetInventoryReservationsQueryHandler(_reservations.Object);
        _reservations
            .Setup(x => x.CountAsync(null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _reservations
            .Setup(x => x.GetAllAsync(null, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<InventoryReservation>());

        var result = await handler.Handle(new GetInventoryReservationsQuery(), CancellationToken.None);

        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }
}
