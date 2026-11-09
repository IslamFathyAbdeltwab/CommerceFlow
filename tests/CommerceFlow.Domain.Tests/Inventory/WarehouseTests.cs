using CommerceFlow.Domain.Entities.Inventory;
using CommerceFlow.Domain.Exceptions;

namespace CommerceFlow.Domain.Tests.Inventory;

public class WarehouseTests
{
    private static Warehouse NewWarehouse() =>
        Warehouse.Create("WH-01", "Main Warehouse", "Alexandria");

    // ---------- Create ----------

    [Fact]
    public void Create_ValidData_StartsActiveWithNormalizedValues()
    {
        var warehouse = Warehouse.Create(" wh-01 ", " Main Warehouse ", "  Alexandria ");

        Assert.Equal("WH-01", warehouse.Code);
        Assert.Equal("Main Warehouse", warehouse.Name);
        Assert.Equal("Alexandria", warehouse.Description);
        Assert.True(warehouse.IsActive);
    }

    [Theory]
    [InlineData("", "Main")]
    [InlineData("   ", "Main")]
    [InlineData("WH-01", "")]
    [InlineData("WH-01", "   ")]
    public void Create_EmptyCodeOrName_Throws(string code, string name)
    {
        Assert.Throws<DomainException>(() => Warehouse.Create(code, name));
    }

    [Fact]
    public void Create_CodeWithSpaces_Throws()
    {
        Assert.Throws<DomainException>(() => Warehouse.Create("WH 01", "Main"));
    }

    [Fact]
    public void Create_CodeLongerThanMaximum_Throws()
    {
        var tooLong = new string('a', Warehouse.CodeMaxLength + 1);
        Assert.Throws<DomainException>(() => Warehouse.Create(tooLong, "Main"));
    }

    [Fact]
    public void Create_BlankDescription_IsStoredAsNull()
    {
        var warehouse = Warehouse.Create("WH-01", "Main", "   ");
        Assert.Null(warehouse.Description);
    }

    // ---------- UpdateDetails ----------

    [Fact]
    public void UpdateDetails_ValidData_UpdatesAllFields()
    {
        var warehouse = NewWarehouse();

        warehouse.UpdateDetails(" wh-02 ", "Backup", "Cairo");

        Assert.Equal("WH-02", warehouse.Code);
        Assert.Equal("Backup", warehouse.Name);
        Assert.Equal("Cairo", warehouse.Description);
    }

    [Fact]
    public void UpdateDetails_EmptyCode_ThrowsAndLeavesUnchanged()
    {
        var warehouse = NewWarehouse();

        Assert.Throws<DomainException>(() => warehouse.UpdateDetails("", "Backup", null));
        Assert.Equal("WH-01", warehouse.Code);
        Assert.Equal("Main Warehouse", warehouse.Name);
    }

    // ---------- Activate / Deactivate ----------

    [Fact]
    public void Deactivate_ActiveWarehouse_BecomesInactive()
    {
        var warehouse = NewWarehouse();
        warehouse.Deactivate();
        Assert.False(warehouse.IsActive);
    }

    [Fact]
    public void Deactivate_AlreadyInactive_DoesNotChangeUpdatedAt()
    {
        var warehouse = NewWarehouse();
        warehouse.Deactivate();
        var updatedAfterFirstCall = warehouse.UpdatedAtUtc;
        warehouse.Deactivate();
        Assert.Equal(updatedAfterFirstCall, warehouse.UpdatedAtUtc);
    }

    [Fact]
    public void Activate_InactiveWarehouse_BecomesActive()
    {
        var warehouse = NewWarehouse();
        warehouse.Deactivate();
        warehouse.Activate();
        Assert.True(warehouse.IsActive);
    }

    [Fact]
    public void Activate_AlreadyActive_DoesNotChangeUpdatedAt()
    {
        var warehouse = NewWarehouse();
        var updatedAfterCreate = warehouse.UpdatedAtUtc;
        warehouse.Activate();
        Assert.Equal(updatedAfterCreate, warehouse.UpdatedAtUtc);
    }
}
