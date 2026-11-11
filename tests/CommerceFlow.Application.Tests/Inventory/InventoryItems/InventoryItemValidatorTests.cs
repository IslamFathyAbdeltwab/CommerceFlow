using CommerceFlow.Application.Inventory.InventoryItems.Commands.AdjustStock;
using CommerceFlow.Application.Inventory.InventoryItems.Commands.ConsumeReservation;
using CommerceFlow.Application.Inventory.InventoryItems.Commands.CreateInventoryItem;
using CommerceFlow.Application.Inventory.InventoryItems.Commands.ReceiveStock;
using CommerceFlow.Application.Inventory.InventoryItems.Commands.ReleaseReservation;
using CommerceFlow.Application.Inventory.InventoryItems.Commands.ReserveStock;
using CommerceFlow.Application.Inventory.InventoryItems.Commands.SetReorderThreshold;
using CommerceFlow.Application.Inventory.InventoryItems.Commands.ShipStock;
using CommerceFlow.Application.Inventory.InventoryItems.Queries.GetInventoryItems;

namespace CommerceFlow.Application.Tests.Inventory.InventoryItems;

public sealed class InventoryItemValidatorTests
{
    [Fact]
    public void CreateInventoryItem_WithValidInput_IsValid()
    {
        var result = new CreateInventoryItemCommandValidator()
            .Validate(new CreateInventoryItemCommand(Guid.NewGuid(), Guid.NewGuid(), 50, 10));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void CreateInventoryItem_WithEmptyIds_IsInvalid()
    {
        var result = new CreateInventoryItemCommandValidator()
            .Validate(new CreateInventoryItemCommand(Guid.Empty, Guid.Empty, 50));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateInventoryItemCommand.WarehouseId));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateInventoryItemCommand.ProductVariantId));
    }

    [Fact]
    public void CreateInventoryItem_WithNegativeQuantities_IsInvalid()
    {
        var result = new CreateInventoryItemCommandValidator()
            .Validate(new CreateInventoryItemCommand(Guid.NewGuid(), Guid.NewGuid(), -1, -1));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateInventoryItemCommand.OnHandQuantity));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateInventoryItemCommand.ReorderThreshold));
    }

    [Fact]
    public void ReceiveStock_WithValidInput_IsValid()
    {
        var result = new ReceiveStockCommandValidator()
            .Validate(new ReceiveStockCommand(Guid.NewGuid(), 10, "Delivery"));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void ReceiveStock_WithNonPositiveQuantity_IsInvalid(int quantity)
    {
        var result = new ReceiveStockCommandValidator()
            .Validate(new ReceiveStockCommand(Guid.NewGuid(), quantity, "Delivery"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ReceiveStockCommand.Quantity));
    }

    [Fact]
    public void ReceiveStock_WithEmptyReason_IsInvalid()
    {
        var result = new ReceiveStockCommandValidator()
            .Validate(new ReceiveStockCommand(Guid.NewGuid(), 10, ""));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ReceiveStockCommand.Reason));
    }

    [Fact]
    public void ReceiveStock_WithReasonOver500Chars_IsInvalid()
    {
        var result = new ReceiveStockCommandValidator()
            .Validate(new ReceiveStockCommand(Guid.NewGuid(), 10, new string('x', 501)));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void ShipStock_WithValidInput_IsValid()
    {
        var result = new ShipStockCommandValidator()
            .Validate(new ShipStockCommand(Guid.NewGuid(), 5, "Order shipment"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ShipStock_WithZeroQuantity_IsInvalid()
    {
        var result = new ShipStockCommandValidator()
            .Validate(new ShipStockCommand(Guid.NewGuid(), 0, "Order shipment"));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void ShipStock_WithEmptyItemId_IsInvalid()
    {
        var result = new ShipStockCommandValidator()
            .Validate(new ShipStockCommand(Guid.Empty, 5, "Order shipment"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ShipStockCommand.InventoryItemId));
    }

    [Fact]
    public void ReserveStock_WithValidInput_IsValid()
    {
        var result = new ReserveStockCommandValidator()
            .Validate(new ReserveStockCommand(Guid.NewGuid(), 3));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ReserveStock_WithZeroQuantity_IsInvalid()
    {
        var result = new ReserveStockCommandValidator()
            .Validate(new ReserveStockCommand(Guid.NewGuid(), 0));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void ReleaseReservation_WithValidInput_IsValid()
    {
        var result = new ReleaseReservationCommandValidator()
            .Validate(new ReleaseReservationCommand(Guid.NewGuid(), 3));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ReleaseReservation_WithNegativeQuantity_IsInvalid()
    {
        var result = new ReleaseReservationCommandValidator()
            .Validate(new ReleaseReservationCommand(Guid.NewGuid(), -3));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void ConsumeReservation_WithValidInput_IsValid()
    {
        var result = new ConsumeReservationCommandValidator()
            .Validate(new ConsumeReservationCommand(Guid.NewGuid(), 3));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ConsumeReservation_WithZeroQuantity_IsInvalid()
    {
        var result = new ConsumeReservationCommandValidator()
            .Validate(new ConsumeReservationCommand(Guid.NewGuid(), 0));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void AdjustStock_WithValidInput_IsValid()
    {
        var result = new AdjustStockCommandValidator()
            .Validate(new AdjustStockCommand(Guid.NewGuid(), -5, "Cycle count"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void AdjustStock_WithZeroChange_IsInvalid()
    {
        var result = new AdjustStockCommandValidator()
            .Validate(new AdjustStockCommand(Guid.NewGuid(), 0, "Cycle count"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(AdjustStockCommand.QuantityChange));
    }

    [Fact]
    public void AdjustStock_WithEmptyReason_IsInvalid()
    {
        var result = new AdjustStockCommandValidator()
            .Validate(new AdjustStockCommand(Guid.NewGuid(), -5, ""));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void SetReorderThreshold_WithValidInput_IsValid()
    {
        var result = new SetReorderThresholdCommandValidator()
            .Validate(new SetReorderThresholdCommand(Guid.NewGuid(), 25));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void SetReorderThreshold_WithNegativeThreshold_IsInvalid()
    {
        var result = new SetReorderThresholdCommandValidator()
            .Validate(new SetReorderThresholdCommand(Guid.NewGuid(), -1));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(SetReorderThresholdCommand.Threshold));
    }

    [Fact]
    public void GetInventoryItems_WithDefaultPaging_IsValid()
    {
        var result = new GetInventoryItemsQueryValidator().Validate(new GetInventoryItemsQuery());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void GetInventoryItems_WithOutOfRangePaging_IsInvalid()
    {
        var result = new GetInventoryItemsQueryValidator()
            .Validate(new GetInventoryItemsQuery(PageNumber: 0, PageSize: 101));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetInventoryItemsQuery.PageNumber));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetInventoryItemsQuery.PageSize));
    }
}
