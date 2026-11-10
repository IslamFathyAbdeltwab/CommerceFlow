using CommerceFlow.Domain.Entities.Orders;
using CommerceFlow.Domain.Exceptions;

namespace CommerceFlow.Domain.Tests.Orders;

// OrderLine has an internal constructor on purpose - lines can only be
// created through the Order aggregate, so every test here goes through Order.
public class OrderLineTests
{
    private static readonly Guid CustomerId = Guid.NewGuid();

    private static Order NewOrder() =>
        Order.Create(CustomerId, AddressSnapshot.Create("John Doe", "123 Main", "Alex", "21500", "Egypt"));

    private static Order NewOrderWithLine(decimal unitPrice = 100m, int quantity = 2)
    {
        var order = NewOrder();
        order.AddLine(Guid.NewGuid(), "SKU-1", "iPhone 15", unitPrice, quantity);
        return order;
    }

    // ---------- AddLine ----------

    [Fact]
    public void AddLine_ValidData_CreatesLineWithSnapshotsAndOrderReference()
    {
        var order = NewOrder();
        var variantId = Guid.NewGuid();

        var line = order.AddLine(variantId, " sku-1 ", " iPhone 15 ", 100m, 2);

        Assert.Equal(order.Id, line.OrderId);
        Assert.Equal(variantId, line.ProductVariantId);
        Assert.Equal("sku-1", line.Sku);
        Assert.Equal("iPhone 15", line.ProductName);
        Assert.Equal(100m, line.UnitPrice);
        Assert.Equal(2, line.Quantity);
        Assert.Equal(200m, line.LineTotal);
        Assert.Single(order.Lines);
    }

    [Fact]
    public void AddLine_UpdatesSubtotal()
    {
        var order = NewOrder();

        order.AddLine(Guid.NewGuid(), "SKU-1", "iPhone 15", 100m, 2);
        order.AddLine(Guid.NewGuid(), "SKU-2", "Case", 50m, 1);

        Assert.Equal(250m, order.Subtotal);
        Assert.Equal(250m, order.GrandTotal);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddLine_InvalidQuantity_Throws(int quantity)
    {
        var order = NewOrder();
        Assert.Throws<DomainException>(() =>
            order.AddLine(Guid.NewGuid(), "SKU-1", "iPhone 15", 100m, quantity));
    }

    [Fact]
    public void AddLine_QuantityAboveMaximum_Throws()
    {
        var order = NewOrder();
        Assert.Throws<DomainException>(() =>
            order.AddLine(Guid.NewGuid(), "SKU-1", "iPhone 15", 100m, OrderLine.MaxQuantity + 1));
    }

    [Fact]
    public void AddLine_EmptyVariantId_Throws()
    {
        var order = NewOrder();
        Assert.Throws<DomainException>(() =>
            order.AddLine(Guid.Empty, "SKU-1", "iPhone 15", 100m, 1));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AddLine_EmptySku_Throws(string sku)
    {
        var order = NewOrder();
        Assert.Throws<DomainException>(() =>
            order.AddLine(Guid.NewGuid(), sku, "iPhone 15", 100m, 1));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AddLine_EmptyProductName_Throws(string productName)
    {
        var order = NewOrder();
        Assert.Throws<DomainException>(() =>
            order.AddLine(Guid.NewGuid(), "SKU-1", productName, 100m, 1));
    }

    [Fact]
    public void AddLine_NegativeUnitPrice_Throws()
    {
        var order = NewOrder();
        Assert.Throws<DomainException>(() =>
            order.AddLine(Guid.NewGuid(), "SKU-1", "iPhone 15", -1m, 1));
    }

    [Fact]
    public void AddLine_ZeroUnitPrice_IsAllowed()
    {
        var order = NewOrder();
        var line = order.AddLine(Guid.NewGuid(), "SKU-1", "Free gift", 0m, 1);
        Assert.Equal(0m, line.LineTotal);
    }

    [Fact]
    public void AddLine_DuplicateVariant_Throws()
    {
        var order = NewOrder();
        var variantId = Guid.NewGuid();
        order.AddLine(variantId, "SKU-1", "iPhone 15", 100m, 1);

        Assert.Throws<DomainException>(() =>
            order.AddLine(variantId, "SKU-1", "iPhone 15", 100m, 2));
    }

    [Fact]
    public void AddLine_SkuLongerThanMaximum_Throws()
    {
        var order = NewOrder();
        var tooLong = new string('s', OrderLine.SkuMaxLength + 1);

        Assert.Throws<DomainException>(() =>
            order.AddLine(Guid.NewGuid(), tooLong, "iPhone 15", 100m, 1));
    }

    [Fact]
    public void AddLine_ProductNameLongerThanMaximum_Throws()
    {
        var order = NewOrder();
        var tooLong = new string('n', OrderLine.ProductNameMaxLength + 1);

        Assert.Throws<DomainException>(() =>
            order.AddLine(Guid.NewGuid(), "SKU-1", tooLong, 100m, 1));
    }

    // ---------- UpdateLineQuantity ----------

    [Fact]
    public void UpdateLineQuantity_ValidQuantity_UpdatesQuantityLineTotalAndSubtotal()
    {
        var order = NewOrderWithLine(100m, 2);
        var line = order.Lines.Single();

        order.UpdateLineQuantity(line.Id, 5);

        Assert.Equal(5, line.Quantity);
        Assert.Equal(500m, line.LineTotal);
        Assert.Equal(500m, order.Subtotal);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    [InlineData(1000)]
    public void UpdateLineQuantity_InvalidQuantity_Throws(int quantity)
    {
        var order = NewOrderWithLine();
        var line = order.Lines.Single();

        Assert.Throws<DomainException>(() => order.UpdateLineQuantity(line.Id, quantity));
        Assert.Equal(2, line.Quantity);
        Assert.Equal(200m, order.Subtotal);
    }

    [Fact]
    public void UpdateLineQuantity_UnknownLine_Throws()
    {
        var order = NewOrderWithLine();
        Assert.Throws<DomainException>(() => order.UpdateLineQuantity(Guid.NewGuid(), 3));
    }

    // ---------- RemoveLine ----------

    [Fact]
    public void RemoveLine_ExistingLine_RemovesAndRecalculatesSubtotal()
    {
        var order = NewOrder();
        var first = order.AddLine(Guid.NewGuid(), "SKU-1", "iPhone 15", 100m, 2);
        order.AddLine(Guid.NewGuid(), "SKU-2", "Case", 50m, 1);

        order.RemoveLine(first.Id);

        Assert.Single(order.Lines);
        Assert.Equal(50m, order.Subtotal);
    }

    [Fact]
    public void RemoveLine_UnknownLine_ThrowsAndKeepsState()
    {
        var order = NewOrderWithLine();

        Assert.Throws<DomainException>(() => order.RemoveLine(Guid.NewGuid()));
        Assert.Single(order.Lines);
        Assert.Equal(200m, order.Subtotal);
    }

    [Fact]
    public void RemoveLine_AllLinesRemoved_SubtotalBecomesZero()
    {
        var order = NewOrderWithLine();
        var line = order.Lines.Single();

        order.RemoveLine(line.Id);

        Assert.Empty(order.Lines);
        Assert.Equal(0m, order.Subtotal);
        Assert.Equal(0m, order.GrandTotal);
    }
}
