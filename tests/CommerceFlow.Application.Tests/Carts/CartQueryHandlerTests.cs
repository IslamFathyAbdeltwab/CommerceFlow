using CommerceFlow.Application.Carts.Interfaces;
using CommerceFlow.Application.Carts.Queries.GetActiveCartByCustomerId;
using CommerceFlow.Application.Carts.Queries.GetCartById;
using CommerceFlow.Application.Carts.Queries.GetCarts;
using CommerceFlow.Domain.Entities.Carts;
using CommerceFlow.Domain.Enums;
using Moq;

namespace CommerceFlow.Application.Tests.Carts;

public sealed class CartQueryHandlerTests
{
    private readonly Mock<ICartRepository> _carts = new();

    // ---------- GetCartById ----------

    [Fact]
    public async Task GetCartById_WhenCartDoesNotExist_ReturnsNull()
    {
        var handler = new GetCartByIdQueryHandler(_carts.Object);
        _carts
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Cart?)null);

        var result = await handler.Handle(new GetCartByIdQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetCartById_WhenCartExists_MapsStatusLinesAndDerivedTotals()
    {
        var handler = new GetCartByIdQueryHandler(_carts.Object);
        var variant1 = Guid.NewGuid();
        var variant2 = Guid.NewGuid();
        var cart = Cart.Create(CartFixtures.CustomerId);
        cart.AddItem(variant1, 2);
        cart.AddItem(variant2, 3);
        _carts
            .Setup(x => x.GetByIdAsync(cart.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        var result = await handler.Handle(new GetCartByIdQuery(cart.Id), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(cart.Id, result.Id);
        Assert.Equal(CartFixtures.CustomerId, result.CustomerId);
        Assert.Equal(CartStatus.Active, result.Status);
        Assert.Equal(2, result.LineCount); // derived, mapped
        Assert.Equal(5, result.TotalQuantity); // derived, mapped
        Assert.Equal(cart.CreatedAtUtc, result.CreatedAtUtc);
        Assert.Equal(cart.UpdatedAtUtc, result.UpdatedAtUtc);

        Assert.Equal(2, result.Lines.Count);
        var entityLines = cart.Lines.ToList();
        Assert.Equal(entityLines[0].Id, result.Lines[0].Id);
        Assert.Equal(variant1, result.Lines[0].ProductVariantId);
        Assert.Equal(2, result.Lines[0].Quantity);
        Assert.Equal(entityLines[0].AddedAtUtc, result.Lines[0].AddedAtUtc);
        Assert.Equal(entityLines[1].Id, result.Lines[1].Id);
        Assert.Equal(variant2, result.Lines[1].ProductVariantId);
        Assert.Equal(3, result.Lines[1].Quantity);
    }

    // ---------- GetActiveCartByCustomerId ----------

    [Fact]
    public async Task GetActiveCart_WhenCustomerHasNoActiveCart_ReturnsNull()
    {
        var handler = new GetActiveCartByCustomerIdQueryHandler(_carts.Object);
        var customerId = Guid.NewGuid();
        _carts
            .Setup(x => x.GetActiveByCustomerIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Cart?)null);

        var result = await handler.Handle(
            new GetActiveCartByCustomerIdQuery(customerId), CancellationToken.None);

        Assert.Null(result); // "no cart yet" - distinct from 404 at the controller
    }

    [Fact]
    public async Task GetActiveCart_WhenActiveCartExists_MapsCart()
    {
        var handler = new GetActiveCartByCustomerIdQueryHandler(_carts.Object);
        var cart = CartFixtures.CreateCart(lines: 2);
        _carts
            .Setup(x => x.GetActiveByCustomerIdAsync(cart.CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        var result = await handler.Handle(
            new GetActiveCartByCustomerIdQuery(cart.CustomerId), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(cart.Id, result.Id);
        Assert.Equal(cart.CustomerId, result.CustomerId);
        Assert.Equal(CartStatus.Active, result.Status);
        Assert.Equal(2, result.LineCount);
        Assert.Equal(2, result.TotalQuantity);
        Assert.Equal(2, result.Lines.Count);
    }

    // ---------- GetCarts ----------

    [Fact]
    public async Task GetCarts_ReturnsPagedListMappedFromRepository()
    {
        var handler = new GetCartsQueryHandler(_carts.Object);
        var cart1 = CartFixtures.CreateCart(lines: 1);
        var cart2 = CartFixtures.CreateCart(lines: 2, customerId: Guid.NewGuid());
        _carts
            .Setup(x => x.CountAsync(
                It.IsAny<Guid?>(), It.IsAny<CartStatus?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);
        _carts
            .Setup(x => x.GetAllAsync(
                It.IsAny<Guid?>(),
                It.IsAny<CartStatus?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Cart> { cart1, cart2 });

        var result = await handler.Handle(new GetCartsQuery(), CancellationToken.None);

        Assert.Equal(42, result.TotalCount);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(20, result.PageSize);
        Assert.Equal(3, result.TotalPages); // ceil(42 / 20)
        Assert.Equal(2, result.Items.Count);

        var first = result.Items[0];
        Assert.Equal(cart1.Id, first.Id);
        Assert.Equal(cart1.CustomerId, first.CustomerId);
        Assert.Equal(CartStatus.Active, first.Status);
        Assert.Equal(1, first.LineCount);
        Assert.Equal(1, first.TotalQuantity);
        Assert.Equal(cart1.UpdatedAtUtc, first.UpdatedAtUtc);

        var second = result.Items[1];
        Assert.Equal(cart2.Id, second.Id);
        Assert.Equal(2, second.LineCount);
        Assert.Equal(2, second.TotalQuantity);
    }

    [Fact]
    public async Task GetCarts_WithFilters_PassesThemThroughToRepository()
    {
        var handler = new GetCartsQueryHandler(_carts.Object);
        var customerId = Guid.NewGuid();
        var cart = CartFixtures.CreateCart(customerId: customerId);
        _carts
            .Setup(x => x.CountAsync(customerId, CartStatus.Active, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _carts
            .Setup(x => x.GetAllAsync(
                customerId, CartStatus.Active, 2, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Cart> { cart });

        var result = await handler.Handle(
            new GetCartsQuery(customerId, CartStatus.Active, 2, 10), CancellationToken.None);

        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal(customerId, result.Items[0].CustomerId);
        _carts.Verify(
            x => x.CountAsync(customerId, CartStatus.Active, It.IsAny<CancellationToken>()),
            Times.Once);
        _carts.Verify(
            x => x.GetAllAsync(customerId, CartStatus.Active, 2, 10, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
