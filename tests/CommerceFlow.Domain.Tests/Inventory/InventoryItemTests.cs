using CommerceFlow.Domain.Entities.Inventory;
using CommerceFlow.Domain.Exceptions;

namespace CommerceFlow.Domain.Tests.Inventory;

public class InventoryItemTests
{
    private static readonly Guid WarehouseId = Guid.NewGuid();
    private static readonly Guid VariantId = Guid.NewGuid();

    private static InventoryItem NewItem(int onHand = 100, int reorder = 10) =>
        InventoryItem.Create(WarehouseId, VariantId, onHand, reorder);

    // ---------- Create ----------

    [Fact]
    public void Create_ValidData_StartsWithZeroReservedAndAvailableEqualsOnHand()
    {
        var item = NewItem(100, 10);

        Assert.Equal(100, item.OnHandQuantity);
        Assert.Equal(0, item.ReservedQuantity);
        Assert.Equal(100, item.AvailableQuantity);
        Assert.Equal(10, item.ReorderThreshold);
        Assert.False(item.IsBelowReorderThreshold);
    }

    [Fact]
    public void Create_NegativeOnHand_Throws()
    {
        Assert.Throws<DomainException>(() => InventoryItem.Create(WarehouseId, VariantId, -1));
    }

    [Fact]
    public void Create_NegativeReorderThreshold_Throws()
    {
        Assert.Throws<DomainException>(() => InventoryItem.Create(WarehouseId, VariantId, 0, -1));
    }

    [Fact]
    public void Create_EmptyWarehouseId_Throws()
    {
        Assert.Throws<DomainException>(() => InventoryItem.Create(Guid.Empty, VariantId, 0));
    }

    [Fact]
    public void Create_EmptyVariantId_Throws()
    {
        Assert.Throws<DomainException>(() => InventoryItem.Create(WarehouseId, Guid.Empty, 0));
    }

    [Fact]
    public void IsBelowReorderThreshold_WhenAvailableEqualsThreshold_IsTrue()
    {
        var item = NewItem(10, 10);
        Assert.True(item.IsBelowReorderThreshold);
    }

    // ---------- ReceiveStock ----------

    [Fact]
    public void ReceiveStock_IncreasesOnHandAndAvailable()
    {
        var item = NewItem(10);
        item.ReceiveStock(5, "Purchase from supplier");
        Assert.Equal(15, item.OnHandQuantity);
        Assert.Equal(15, item.AvailableQuantity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ReceiveStock_NonPositiveQuantity_Throws(int qty)
    {
        var item = NewItem();
        Assert.Throws<DomainException>(() => item.ReceiveStock(qty, "reason"));
    }

    [Fact]
    public void ReceiveStock_EmptyReason_Throws()
    {
        var item = NewItem();
        Assert.Throws<DomainException>(() => item.ReceiveStock(1, "   "));
    }

    // ---------- ShipStock / WriteOffStock ----------

    [Fact]
    public void ShipStock_ReducesOnHandByQuantity()
    {
        var item = NewItem(100);
        item.ShipStock(30, "Order shipped");
        Assert.Equal(70, item.OnHandQuantity);
        Assert.Equal(70, item.AvailableQuantity);
    }

    [Fact]
    public void ShipStock_MoreThanAvailable_ThrowsAndKeepsQuantities()
    {
        var item = NewItem(10);
        item.Reserve(5);

        Assert.Throws<DomainException>(() => item.ShipStock(6, "Order"));
        Assert.Equal(10, item.OnHandQuantity);
        Assert.Equal(5, item.ReservedQuantity);
    }

    [Fact]
    public void WriteOffStock_ReducesOnHand()
    {
        var item = NewItem(20);
        item.WriteOffStock(5, "Damaged item");
        Assert.Equal(15, item.OnHandQuantity);
    }

    [Fact]
    public void WriteOffStock_MoreThanAvailable_Throws()
    {
        var item = NewItem(5);
        Assert.Throws<DomainException>(() => item.WriteOffStock(6, "Damaged"));
    }

    // ---------- AdjustStock ----------

    [Fact]
    public void AdjustStock_Positive_IncreasesOnHand()
    {
        var item = NewItem(10);
        item.AdjustStock(5, "Found in recount");
        Assert.Equal(15, item.OnHandQuantity);
    }

    [Fact]
    public void AdjustStock_Negative_DecreasesOnHand()
    {
        var item = NewItem(10);
        item.AdjustStock(-3, "Found loss");
        Assert.Equal(7, item.OnHandQuantity);
    }

    [Fact]
    public void AdjustStock_Zero_Throws()
    {
        var item = NewItem(10);
        Assert.Throws<DomainException>(() => item.AdjustStock(0, "no change"));
    }

    [Fact]
    public void AdjustStock_NegativeBeyondAvailable_Throws()
    {
        var item = NewItem(5);
        item.Reserve(2);

        Assert.Throws<DomainException>(() => item.AdjustStock(-4, "loss"));
        Assert.Equal(5, item.OnHandQuantity);
        Assert.Equal(2, item.ReservedQuantity);
    }

    // ---------- Reserve / Release / Consume ----------

    [Fact]
    public void Reserve_IncreasesReservedAndDecreasesAvailable()
    {
        var item = NewItem(100);
        item.Reserve(30);

        Assert.Equal(30, item.ReservedQuantity);
        Assert.Equal(70, item.AvailableQuantity);
        Assert.Equal(100, item.OnHandQuantity);
    }

    [Fact]
    public void Reserve_MoreThanAvailable_Throws()
    {
        var item = NewItem(10);
        Assert.Throws<DomainException>(() => item.Reserve(11));
    }

    [Fact]
    public void ReleaseReservation_DecreasesReserved()
    {
        var item = NewItem(100);
        item.Reserve(40);
        item.ReleaseReservation(15);

        Assert.Equal(25, item.ReservedQuantity);
        Assert.Equal(75, item.AvailableQuantity);
    }

    [Fact]
    public void ReleaseReservation_MoreThanReserved_Throws()
    {
        var item = NewItem(100);
        item.Reserve(5);
        Assert.Throws<DomainException>(() => item.ReleaseReservation(6));
    }

    [Fact]
    public void ConsumeReservation_ReducesBothReservedAndOnHand()
    {
        var item = NewItem(100);
        item.Reserve(30);
        item.ConsumeReservation(20);

        Assert.Equal(10, item.ReservedQuantity);
        Assert.Equal(80, item.OnHandQuantity);
        Assert.Equal(70, item.AvailableQuantity);
    }

    [Fact]
    public void ConsumeReservation_MoreThanReserved_Throws()
    {
        var item = NewItem(100);
        item.Reserve(10);
        Assert.Throws<DomainException>(() => item.ConsumeReservation(11));
    }

    // ---------- SetReorderThreshold ----------

    [Fact]
    public void SetReorderThreshold_Valid_UpdatesAndAffectsLowStockFlag()
    {
        var item = NewItem(10, 5);
        Assert.False(item.IsBelowReorderThreshold);

        item.SetReorderThreshold(15);
        Assert.Equal(15, item.ReorderThreshold);
        Assert.True(item.IsBelowReorderThreshold);
    }

    [Fact]
    public void SetReorderThreshold_Negative_Throws()
    {
        var item = NewItem();
        Assert.Throws<DomainException>(() => item.SetReorderThreshold(-1));
    }
}
