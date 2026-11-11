using CommerceFlow.Application.Inventory.Interfaces;
using CommerceFlow.Application.Inventory.StockMovements.Queries.GetStockMovementById;
using CommerceFlow.Application.Inventory.StockMovements.Queries.GetStockMovements;
using CommerceFlow.Domain.Entities.Inventory;
using CommerceFlow.Domain.Enums;
using Moq;

namespace CommerceFlow.Application.Tests.Inventory.StockMovements;

public sealed class StockMovementQueryHandlerTests
{
    private readonly Mock<IStockMovementRepository> _movements = new();

    private static StockMovement CreateMovement(
        StockMovementType type = StockMovementType.StockReceived,
        int quantityChange = 10)
    {
        return StockMovement.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            type,
            quantityChange,
            100,
            "Restock delivery",
            referenceType: "PurchaseOrder",
            referenceId: Guid.NewGuid(),
            performedByUserId: Guid.NewGuid());
    }

    [Fact]
    public async Task GetStockMovementById_WhenMovementDoesNotExist_ReturnsNull()
    {
        var handler = new GetStockMovementByIdQueryHandler(_movements.Object);
        _movements
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((StockMovement?)null);

        var result = await handler.Handle(new GetStockMovementByIdQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetStockMovementById_WhenMovementExists_MapsAllFields()
    {
        var handler = new GetStockMovementByIdQueryHandler(_movements.Object);
        var movement = CreateMovement(StockMovementType.ManualAdjustment, quantityChange: -5);
        _movements
            .Setup(x => x.GetByIdAsync(movement.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(movement);

        var result = await handler.Handle(new GetStockMovementByIdQuery(movement.Id), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(movement.Id, result.Id);
        Assert.Equal(movement.WarehouseId, result.WarehouseId);
        Assert.Equal(movement.ProductVariantId, result.ProductVariantId);
        Assert.Equal(StockMovementType.ManualAdjustment, result.MovementType);
        Assert.Equal(-5, result.QuantityChange);
        Assert.Equal(100, result.QuantityAfter);
        Assert.Equal("PurchaseOrder", result.ReferenceType);
        Assert.Equal(movement.ReferenceId, result.ReferenceId);
        Assert.Equal("Restock delivery", result.Reason);
        Assert.Equal(movement.PerformedByUserId, result.PerformedByUserId);
        Assert.Equal(movement.CreatedAtUtc, result.CreatedAtUtc);
    }

    [Fact]
    public async Task GetStockMovements_ReturnsPagedResultWithFiltersPassedThrough()
    {
        var handler = new GetStockMovementsQueryHandler(_movements.Object);
        var warehouseId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var received = CreateMovement(StockMovementType.StockReceived, 20);
        var shipment = CreateMovement(StockMovementType.Shipment, -4);
        _movements
            .Setup(x => x.CountAsync(warehouseId, variantId, StockMovementType.Shipment, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(12);
        _movements
            .Setup(x => x.GetAllAsync(warehouseId, variantId, StockMovementType.Shipment, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StockMovement> { received, shipment });

        var result = await handler.Handle(
            new GetStockMovementsQuery(WarehouseId: warehouseId, ProductVariantId: variantId, MovementType: StockMovementType.Shipment),
            CancellationToken.None);

        Assert.Equal(12, result.TotalCount);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(20, result.PageSize);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(StockMovementType.StockReceived, result.Items[0].MovementType);
        Assert.Equal(20, result.Items[0].QuantityChange);
        Assert.Equal(StockMovementType.Shipment, result.Items[1].MovementType);
        Assert.Equal(-4, result.Items[1].QuantityChange);
        _movements.Verify(
            x => x.CountAsync(warehouseId, variantId, StockMovementType.Shipment, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetStockMovements_WithNoFilters_ReturnsEverythingMapped()
    {
        var handler = new GetStockMovementsQueryHandler(_movements.Object);
        var movement = CreateMovement();
        _movements
            .Setup(x => x.CountAsync(null, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _movements
            .Setup(x => x.GetAllAsync(null, null, null, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StockMovement> { movement });

        var result = await handler.Handle(new GetStockMovementsQuery(), CancellationToken.None);

        var item = Assert.Single(result.Items);
        Assert.Equal(movement.Id, item.Id);
        Assert.Equal(movement.CreatedAtUtc, item.CreatedAtUtc);
        Assert.Equal(1, result.TotalCount);
    }
}
