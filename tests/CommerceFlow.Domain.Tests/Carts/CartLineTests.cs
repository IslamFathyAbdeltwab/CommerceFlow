using CommerceFlow.Domain.Entities.Carts;
using CommerceFlow.Domain.Enums;
using CommerceFlow.Domain.Exceptions;

namespace CommerceFlow.Domain.Tests.Carts;

// CartLine has an internal constructor on purpose - lines can only be
// created through the Cart aggregate, so every test here goes through Cart.
public class CartLineTests
{
    private static readonly Guid CustomerId = Guid.NewGuid();

    private static Cart NewCart() => Cart.Create(CustomerId);

    // ---------- AddItem ----------

    [Fact]
    public void AddItem_ValidData_CreatesLineWithCartReferenceAndTimestamp()
    {
        var cart = NewCart();
        var variantId = Guid.NewGuid();

        var line = cart.AddItem(variantId, 3);

        Assert.Equal(cart.Id, line.CartId);
        Assert.Equal(variantId, line.ProductVariantId);
        Assert.Equal(3, line.Quantity);
        Assert.NotEqual(Guid.Empty, line.Id);
        Assert.NotEqual(default, line.AddedAtUtc);
        Assert.True((DateTimeOffset.UtcNow - line.AddedAtUtc) < TimeSpan.FromMinutes(1));
        Assert.Single(cart.Lines);
    }

    [Fact]
    public void AddItem_ExistingVariant_IncrementsQuantityInsteadOfDuplicating()
    {
        var cart = NewCart();
        var variantId = Guid.NewGuid();

        var first = cart.AddItem(variantId, 3);
        var second = cart.AddItem(variantId, 2);

        Assert.Same(first, second);
        Assert.Equal(5, second.Quantity);
        Assert.Single(cart.Lines);
        Assert.Equal(5, cart.TotalQuantity);
    }

    [Fact]
    public void AddItem_MergingBeyondMaxQuantity_ThrowsAndKeepsQuantity()
    {
        var cart = NewCart();
        var variantId = Guid.NewGuid();
        cart.AddItem(variantId, CartLine.MaxQuantity);

        Assert.Throws<DomainException>(() => cart.AddItem(variantId, 1));

        Assert.Equal(CartLine.MaxQuantity, cart.Lines.Single().Quantity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddItem_InvalidQuantity_Throws(int quantity)
    {
        var cart = NewCart();

        Assert.Throws<DomainException>(() => cart.AddItem(Guid.NewGuid(), quantity));
        Assert.Empty(cart.Lines);
    }

    [Fact]
    public void AddItem_QuantityAboveMaximum_Throws()
    {
        var cart = NewCart();

        Assert.Throws<DomainException>(() =>
            cart.AddItem(Guid.NewGuid(), CartLine.MaxQuantity + 1));
        Assert.Empty(cart.Lines);
    }

    [Fact]
    public void AddItem_EmptyVariantId_Throws()
    {
        var cart = NewCart();

        Assert.Throws<DomainException>(() => cart.AddItem(Guid.Empty, 1));
        Assert.Empty(cart.Lines);
    }

    // ---------- ChangeQuantity ----------

    [Fact]
    public void ChangeQuantity_Valid_UpdatesQuantityAndDerivedTotal()
    {
        var cart = NewCart();
        var line = cart.AddItem(Guid.NewGuid(), 2);
        cart.AddItem(Guid.NewGuid(), 3);

        cart.ChangeQuantity(line.Id, 10);

        Assert.Equal(10, line.Quantity);
        Assert.Equal(13, cart.TotalQuantity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1000)]
    public void ChangeQuantity_InvalidQuantity_ThrowsAndKeepsQuantity(int quantity)
    {
        var cart = NewCart();
        var line = cart.AddItem(Guid.NewGuid(), 2);

        Assert.Throws<DomainException>(() => cart.ChangeQuantity(line.Id, quantity));

        Assert.Equal(2, line.Quantity);
    }

    [Fact]
    public void ChangeQuantity_UnknownLine_Throws()
    {
        var cart = NewCart();
        cart.AddItem(Guid.NewGuid(), 2);

        Assert.Throws<DomainException>(() => cart.ChangeQuantity(Guid.NewGuid(), 5));
    }

    // ---------- RemoveItem ----------

    [Fact]
    public void RemoveItem_Valid_RemovesOnlyThatLine()
    {
        var cart = NewCart();
        var first = cart.AddItem(Guid.NewGuid(), 2);
        var second = cart.AddItem(Guid.NewGuid(), 3);

        cart.RemoveItem(first.Id);

        var remaining = Assert.Single(cart.Lines);
        Assert.Equal(second.Id, remaining.Id);
        Assert.Equal(3, cart.TotalQuantity);
    }

    [Fact]
    public void RemoveItem_UnknownLine_Throws()
    {
        var cart = NewCart();
        cart.AddItem(Guid.NewGuid(), 2);

        Assert.Throws<DomainException>(() => cart.RemoveItem(Guid.NewGuid()));
        Assert.Single(cart.Lines);
    }
}
