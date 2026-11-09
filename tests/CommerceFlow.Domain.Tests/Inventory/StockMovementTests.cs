using CommerceFlow.Domain.Entities.Inventory;
using CommerceFlow.Domain.Enums;
using CommerceFlow.Domain.Exceptions;

namespace CommerceFlow.Domain.Tests.Inventory;

public class StockMovementTests
{
    private static readonly Guid WarehouseId = Guid.NewGuid();
    private static readonly Guid VariantId = Guid.NewGuid();

    // ---------- Create ----------

    [Fact]
    public void Create_ValidData_StoresAllFields()
    {
        var movement = StockMovement.Create(
            WarehouseId,
            VariantId,
            StockMovementType.StockReceived,
            50,
            50,
            "Purchase order PO-100",
            "PurchaseOrder",
            Guid.NewGuid(),
            Guid.NewGuid());

        Assert.Equal(WarehouseId, movement.WarehouseId);
        Assert.Equal(VariantId, movement.ProductVariantId);
        Assert.Equal(StockMovementType.StockReceived, movement.MovementType);
        Assert.Equal(50, movement.QuantityChange);
        Assert.Equal(50, movement.QuantityAfter);
        Assert.Equal("PurchaseOrder", movement.ReferenceType);
        Assert.Equal("Purchase order PO-100", movement.Reason);
        Assert.NotEqual(default(DateTimeOffset), movement.CreatedAtUtc);
    }

    [Fact]
    public void Create_ZeroQuantityChange_Throws()
    {
        Assert.Throws<DomainException>(() => StockMovement.Create(
            WarehouseId, VariantId, StockMovementType.ManualAdjustment, 0, 10, "fix"));
    }

    [Fact]
    public void Create_NegativeQuantityAfter_Throws()
    {
        Assert.Throws<DomainException>(() => StockMovement.Create(
            WarehouseId, VariantId, StockMovementType.ManualAdjustment, -5, -1, "loss"));
    }

    [Fact]
    public void Create_EmptyReason_Throws()
    {
        Assert.Throws<DomainException>(() => StockMovement.Create(
            WarehouseId, VariantId, StockMovementType.ManualAdjustment, 1, 10, "   "));
    }

    [Fact]
    public void Create_InvalidMovementType_Throws()
    {
        Assert.Throws<DomainException>(() => StockMovement.Create(
            WarehouseId, VariantId, (StockMovementType)999, 1, 10, "reason"));
    }

    [Theory]
    [InlineData(StockMovementType.StockReceived)]
    [InlineData(StockMovementType.Shipment)]
    [InlineData(StockMovementType.ReturnRestock)]
    [InlineData(StockMovementType.ManualAdjustment)]
    [InlineData(StockMovementType.DamagedWriteOff)]
    public void Create_AllMovementTypes_AreAccepted(StockMovementType type)
    {
        var movement = StockMovement.Create(WarehouseId, VariantId, type, 1, 10, "reason");
        Assert.Equal(type, movement.MovementType);
    }
}
