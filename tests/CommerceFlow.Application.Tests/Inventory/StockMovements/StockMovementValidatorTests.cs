using CommerceFlow.Application.Inventory.StockMovements.Queries.GetStockMovements;
using CommerceFlow.Domain.Enums;

namespace CommerceFlow.Application.Tests.Inventory.StockMovements;

public sealed class StockMovementValidatorTests
{
    [Fact]
    public void GetStockMovements_WithDefaultPaging_IsValid()
    {
        var result = new GetStockMovementsQueryValidator().Validate(new GetStockMovementsQuery());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void GetStockMovements_WithOutOfRangePaging_IsInvalid()
    {
        var result = new GetStockMovementsQueryValidator()
            .Validate(new GetStockMovementsQuery(PageNumber: 0, PageSize: 101));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetStockMovementsQuery.PageNumber));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetStockMovementsQuery.PageSize));
    }

    [Fact]
    public void GetStockMovements_WithValidFilters_IsValid()
    {
        var result = new GetStockMovementsQueryValidator()
            .Validate(new GetStockMovementsQuery(
                WarehouseId: Guid.NewGuid(),
                ProductVariantId: Guid.NewGuid(),
                MovementType: StockMovementType.Shipment,
                ReferenceId: Guid.NewGuid()));

        Assert.True(result.IsValid);
    }
}
