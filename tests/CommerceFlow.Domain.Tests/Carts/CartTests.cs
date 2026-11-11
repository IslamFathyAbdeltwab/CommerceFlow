using CommerceFlow.Domain.Entities.Carts;
using CommerceFlow.Domain.Enums;
using CommerceFlow.Domain.Exceptions;

namespace CommerceFlow.Domain.Tests.Carts;

public class CartTests
{
    private static readonly Guid CustomerId = Guid.NewGuid();

    private static Cart NewCart() => Cart.Create(CustomerId);

    private static Cart NewCartWithItem(int quantity = 2)
    {
        var cart = NewCart();
        cart.AddItem(Guid.NewGuid(), quantity);
        return cart;
    }

    // ---------- Create ----------

    [Fact]
    public void Create_ValidData_CreatesActiveEmptyCartForCustomer()
    {
        var cart = Cart.Create(CustomerId);

        Assert.Equal(CustomerId, cart.CustomerId);
        Assert.Equal(CartStatus.Active, cart.Status);
        Assert.Empty(cart.Lines);
        Assert.Equal(0, cart.LineCount);
        Assert.Equal(0, cart.TotalQuantity);
        Assert.NotEqual(Guid.Empty, cart.Id);
    }

    [Fact]
    public void Create_SetsAuditTimestamps()
    {
        var cart = Cart.Create(CustomerId);

        Assert.NotEqual(default, cart.CreatedAtUtc);
        Assert.NotEqual(default, cart.UpdatedAtUtc);
        Assert.True(cart.UpdatedAtUtc >= cart.CreatedAtUtc);
    }

    [Fact]
    public void Create_EmptyCustomerId_Throws()
    {
        Assert.Throws<DomainException>(() => Cart.Create(Guid.Empty));
    }

    // ---------- Derived counts ----------

    [Fact]
    public void LineCountAndTotalQuantity_ReflectLineState()
    {
        var cart = NewCart();
        cart.AddItem(Guid.NewGuid(), 2);
        cart.AddItem(Guid.NewGuid(), 3);

        Assert.Equal(2, cart.LineCount);
        Assert.Equal(5, cart.TotalQuantity);

        cart.RemoveItem(cart.Lines.First().Id);

        Assert.Equal(1, cart.LineCount);
        Assert.Equal(3, cart.TotalQuantity);
    }

    // ---------- Active guards on line operations ----------

    [Fact]
    public void AddItem_WhenNotActive_Throws()
    {
        var cart = NewCartWithItem();
        cart.MarkCheckedOut();

        Assert.Throws<DomainException>(() => cart.AddItem(Guid.NewGuid(), 1));
    }

    [Fact]
    public void ChangeQuantity_WhenNotActive_Throws()
    {
        var cart = NewCartWithItem();
        cart.MarkCheckedOut();
        var lineId = cart.Lines.Single().Id;

        Assert.Throws<DomainException>(() => cart.ChangeQuantity(lineId, 5));
        Assert.Equal(2, cart.Lines.Single().Quantity);
    }

    [Fact]
    public void RemoveItem_WhenNotActive_Throws()
    {
        var cart = NewCartWithItem();
        cart.MarkCheckedOut();
        var lineId = cart.Lines.Single().Id;

        Assert.Throws<DomainException>(() => cart.RemoveItem(lineId));
        Assert.Single(cart.Lines);
    }

    // ---------- Max lines ----------

    [Fact]
    public void AddItem_ExceedingMaxLines_Throws()
    {
        var cart = NewCart();
        for (var i = 0; i < Cart.MaxLines; i++)
            cart.AddItem(Guid.NewGuid(), 1);

        Assert.Equal(Cart.MaxLines, cart.LineCount);

        Assert.Throws<DomainException>(() => cart.AddItem(Guid.NewGuid(), 1));
        Assert.Equal(Cart.MaxLines, cart.LineCount);
    }

    // ---------- Clear ----------

    [Fact]
    public void Clear_RemovesAllLines()
    {
        var cart = NewCart();
        cart.AddItem(Guid.NewGuid(), 2);
        cart.AddItem(Guid.NewGuid(), 3);

        cart.Clear();

        Assert.Empty(cart.Lines);
        Assert.Equal(0, cart.LineCount);
        Assert.Equal(0, cart.TotalQuantity);
    }

    [Fact]
    public void Clear_OnEmptyCart_IsNoOp()
    {
        var cart = NewCart();

        cart.Clear();

        Assert.Empty(cart.Lines);
        Assert.Equal(CartStatus.Active, cart.Status);
    }

    [Fact]
    public void Clear_WhenNotActive_Throws()
    {
        var cart = NewCartWithItem();
        cart.MarkAbandoned();

        Assert.Throws<DomainException>(() => cart.Clear());
        Assert.Single(cart.Lines);
    }

    // ---------- Lifecycle ----------

    [Fact]
    public void MarkCheckedOut_WhenActiveWithItems_TransitionsToCheckedOut()
    {
        var cart = NewCartWithItem();

        cart.MarkCheckedOut();

        Assert.Equal(CartStatus.CheckedOut, cart.Status);
    }

    [Fact]
    public void MarkCheckedOut_OnEmptyCart_Throws()
    {
        var cart = NewCart();

        Assert.Throws<DomainException>(() => cart.MarkCheckedOut());
        Assert.Equal(CartStatus.Active, cart.Status);
    }

    [Fact]
    public void MarkCheckedOut_Twice_Throws()
    {
        var cart = NewCartWithItem();
        cart.MarkCheckedOut();

        Assert.Throws<DomainException>(() => cart.MarkCheckedOut());
        Assert.Equal(CartStatus.CheckedOut, cart.Status);
    }

    [Fact]
    public void MarkCheckedOut_WhenAbandoned_Throws()
    {
        var cart = NewCartWithItem();
        cart.MarkAbandoned();

        Assert.Throws<DomainException>(() => cart.MarkCheckedOut());
        Assert.Equal(CartStatus.Abandoned, cart.Status);
    }

    [Fact]
    public void MarkAbandoned_WhenActive_TransitionsToAbandoned()
    {
        var cart = NewCart(); // an empty cart may still be abandoned

        cart.MarkAbandoned();

        Assert.Equal(CartStatus.Abandoned, cart.Status);
    }

    [Fact]
    public void MarkAbandoned_WhenCheckedOut_Throws()
    {
        var cart = NewCartWithItem();
        cart.MarkCheckedOut();

        Assert.Throws<DomainException>(() => cart.MarkAbandoned());
        Assert.Equal(CartStatus.CheckedOut, cart.Status);
    }

    [Fact]
    public void MarkAbandoned_Twice_Throws()
    {
        var cart = NewCart();
        cart.MarkAbandoned();

        Assert.Throws<DomainException>(() => cart.MarkAbandoned());
        Assert.Equal(CartStatus.Abandoned, cart.Status);
    }
}
