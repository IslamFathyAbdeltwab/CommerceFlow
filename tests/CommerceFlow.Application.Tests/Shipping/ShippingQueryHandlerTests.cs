using CommerceFlow.Application.Shipping.Interfaces;
using CommerceFlow.Application.Shipping.Queries.GetShipmentById;
using CommerceFlow.Application.Shipping.Queries.GetShipments;
using CommerceFlow.Domain.Entities.Shipping;
using CommerceFlow.Domain.Enums;
using Moq;

namespace CommerceFlow.Application.Tests.Shipping;

public sealed class ShippingQueryHandlerTests
{
    private readonly Mock<IShipmentRepository> _shipments = new();

    private void SetupShipment(Shipment shipment) =>
        _shipments
            .Setup(x => x.GetByIdAsync(shipment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shipment);

    // ---------- GetShipmentById ----------

    [Fact]
    public async Task GetShipmentById_WhenShipmentDoesNotExist_ReturnsNull()
    {
        var handler = new GetShipmentByIdQueryHandler(_shipments.Object);

        var result = await handler.Handle(
            new GetShipmentByIdQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetShipmentById_WhenShipmentExists_MapsAllFields()
    {
        var handler = new GetShipmentByIdQueryHandler(_shipments.Object);
        var shipment = ShippingFixtures.CreateShipment(
            status: ShipmentStatus.Failed,
            carrier: "DHL",
            trackingNumber: "TRACK-001",
            serviceLevel: "Express",
            failureReason: "Nobody home.");
        SetupShipment(shipment);

        var dto = await handler.Handle(
            new GetShipmentByIdQuery(shipment.Id), CancellationToken.None);

        Assert.NotNull(dto);
        Assert.Equal(shipment.Id, dto.Id);
        Assert.Equal(shipment.OrderId, dto.OrderId);
        Assert.Equal(ShipmentStatus.Failed, dto.Status);
        Assert.Equal("DHL", dto.Carrier);
        Assert.Equal("TRACK-001", dto.TrackingNumber);
        Assert.Equal("Express", dto.ServiceLevel);
        Assert.Equal("Nobody home.", dto.FailureReason);
        Assert.NotNull(dto.ShippedAtUtc);
        Assert.Null(dto.DeliveredAtUtc);
        Assert.Null(dto.CancelledAtUtc);
        Assert.Equal(shipment.CreatedAtUtc, dto.CreatedAtUtc);
        Assert.Equal(shipment.UpdatedAtUtc, dto.UpdatedAtUtc);

        var item = Assert.Single(dto.Items);
        var entity = Assert.Single(shipment.Items);
        Assert.Equal(entity.Id, item.Id);
        Assert.Equal(entity.ProductVariantId, item.ProductVariantId);
        Assert.Equal(entity.Quantity, item.Quantity);
    }

    [Fact]
    public async Task GetShipmentById_WhenDelivered_MapsDeliveryTimestamp()
    {
        var handler = new GetShipmentByIdQueryHandler(_shipments.Object);
        var shipment = ShippingFixtures.CreateShipment(status: ShipmentStatus.Delivered);
        SetupShipment(shipment);

        var dto = await handler.Handle(
            new GetShipmentByIdQuery(shipment.Id), CancellationToken.None);

        Assert.NotNull(dto);
        Assert.Equal(ShipmentStatus.Delivered, dto.Status);
        Assert.NotNull(dto.DeliveredAtUtc);
        Assert.Null(dto.FailureReason);
    }

    [Fact]
    public async Task GetShipmentById_WhenCancelled_MapsCancellationTimestamp()
    {
        var handler = new GetShipmentByIdQueryHandler(_shipments.Object);
        var shipment = ShippingFixtures.CreateShipment(status: ShipmentStatus.Cancelled);
        SetupShipment(shipment);

        var dto = await handler.Handle(
            new GetShipmentByIdQuery(shipment.Id), CancellationToken.None);

        Assert.NotNull(dto);
        Assert.Equal(ShipmentStatus.Cancelled, dto.Status);
        Assert.NotNull(dto.CancelledAtUtc);
        Assert.Null(dto.ShippedAtUtc);
    }

    // ---------- GetShipments ----------

    [Fact]
    public async Task GetShipments_WhenNoneExist_ReturnsEmptyPagedResult()
    {
        var handler = new GetShipmentsQueryHandler(_shipments.Object);
        _shipments
            .Setup(x => x.CountAsync(
                It.IsAny<Guid?>(), It.IsAny<ShipmentStatus?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _shipments
            .Setup(x => x.GetAllAsync(
                It.IsAny<Guid?>(), It.IsAny<ShipmentStatus?>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Shipment>());

        var result = await handler.Handle(new GetShipmentsQuery(), CancellationToken.None);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(20, result.PageSize);
        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    public async Task GetShipments_WithFilters_PassesThemThroughAndMapsItems()
    {
        var handler = new GetShipmentsQueryHandler(_shipments.Object);
        var orderId = Guid.NewGuid();
        var first = ShippingFixtures.CreateShipment(
            orderId: orderId, status: ShipmentStatus.Pending);
        var second = ShippingFixtures.CreateShipment(
            orderId: orderId, status: ShipmentStatus.Shipped);
        _shipments
            .Setup(x => x.CountAsync(
                orderId, ShipmentStatus.Pending, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        _shipments
            .Setup(x => x.GetAllAsync(
                orderId, ShipmentStatus.Pending, 2, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Shipment> { first, second });

        var result = await handler.Handle(
            new GetShipmentsQuery(orderId, ShipmentStatus.Pending, 2, 5),
            CancellationToken.None);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(5, result.PageSize);
        Assert.Equal(
            new[] { first.Id, second.Id },
            result.Items.Select(i => i.Id));
        Assert.Equal(
            new[] { ShipmentStatus.Pending, ShipmentStatus.Shipped },
            result.Items.Select(i => i.Status));

        _shipments.Verify(
            x => x.CountAsync(orderId, ShipmentStatus.Pending, It.IsAny<CancellationToken>()),
            Times.Once);
        _shipments.Verify(
            x => x.GetAllAsync(
                orderId, ShipmentStatus.Pending, 2, 5, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
