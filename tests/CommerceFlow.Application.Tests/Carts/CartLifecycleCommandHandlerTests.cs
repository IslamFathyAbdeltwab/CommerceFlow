using CommerceFlow.Application.Carts.Commands.AbandonCart;
using CommerceFlow.Application.Carts.Commands.CheckoutCart;
using CommerceFlow.Application.Carts.Interfaces;
using CommerceFlow.Application.Catalog.Interfaces;
using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Orders.Interfaces;
using CommerceFlow.Domain.Entities.Carts;
using CommerceFlow.Domain.Entities.Orders;
using CommerceFlow.Domain.Enums;
using Moq;

namespace CommerceFlow.Application.Tests.Carts;

public sealed class CartLifecycleCommandHandlerTests
{
    private readonly Mock<ICartRepository> _carts = new();
    private readonly Mock<IProductRepository> _products = new();
    private readonly Mock<IOrderRepository> _orders = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private void SetupCart(Cart cart) =>
        _carts
            .Setup(x => x.GetByIdAsync(cart.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

    private CheckoutCartCommandHandler CreateCheckoutHandler() =>
        new(_carts.Object, _products.Object, _orders.Object, _unitOfWork.Object);

    // The catalog answers for every variant the cart asks about, at a fixed price.
    private void SetupCatalog(decimal unitPrice = 25m) =>
        _products
            .Setup(x => x.GetVariantSnapshotsAsync(
                It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .Returns((IReadOnlyCollection<Guid> ids, CancellationToken _) =>
                Task.FromResult<IReadOnlyList<VariantCheckoutSnapshot>>(
                    ids.Select(id => new VariantCheckoutSnapshot(id, $"SKU-{id}", "Test product", unitPrice))
                       .ToList()));

    // ---------- Checkout ----------

    [Fact]
    public async Task Checkout_WhenCartDoesNotExist_ThrowsNotFound()
    {
        var handler = CreateCheckoutHandler();
        var cartId = Guid.NewGuid();
        _carts
            .Setup(x => x.GetByIdAsync(cartId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Cart?)null);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new CheckoutCartCommand(cartId, CartFixtures.ValidAddress()),
                CancellationToken.None));

        Assert.Contains("Cart", exception.Message);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Checkout_WhenCartIsNotActive_ThrowsConflict()
    {
        var handler = CreateCheckoutHandler();
        var cart = CartFixtures.CreateCart(CartStatus.CheckedOut);
        SetupCart(cart);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(
                new CheckoutCartCommand(cart.Id, CartFixtures.ValidAddress()),
                CancellationToken.None));

        Assert.Contains(
            "Only active carts can be checked out (current: CheckedOut).",
            exception.Message);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Checkout_WhenCartIsEmpty_ThrowsConflict()
    {
        var handler = CreateCheckoutHandler();
        var cart = CartFixtures.CreateCart(); // active but no lines
        SetupCart(cart);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(
                new CheckoutCartCommand(cart.Id, CartFixtures.ValidAddress()),
                CancellationToken.None));

        Assert.Contains("Cannot check out an empty cart.", exception.Message);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Checkout_WhenVariantIsGoneFromCatalog_ThrowsConflictWithoutSaving()
    {
        var handler = CreateCheckoutHandler();
        var cart = CartFixtures.CreateCart(lines: 2);
        SetupCart(cart);
        _products
            .Setup(x => x.GetVariantSnapshotsAsync(
                It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<VariantCheckoutSnapshot>());

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(
                new CheckoutCartCommand(cart.Id, CartFixtures.ValidAddress()),
                CancellationToken.None));

        Assert.Contains("2 product variant(s) no longer exist", exception.Message);
        Assert.Equal(CartStatus.Active, cart.Status); // cart untouched, still buyable
        _orders.Verify(
            x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Checkout_WhenCatalogReadFails_LeavesCartActiveAndDoesNotSave()
    {
        var handler = CreateCheckoutHandler();
        var cart = CartFixtures.CreateCart(lines: 1);
        SetupCart(cart);
        _products
            .Setup(x => x.GetVariantSnapshotsAsync(
                It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("catalog unreachable"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(
                new CheckoutCartCommand(cart.Id, CartFixtures.ValidAddress()),
                CancellationToken.None));

        Assert.Equal(CartStatus.Active, cart.Status); // conversion happens after the read
        _orders.Verify(
            x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Checkout_WhenActiveWithLines_CreatesOrderFromCatalogAndSavesOnce()
    {
        var handler = CreateCheckoutHandler();
        var cart = CartFixtures.CreateCart(lines: 2);
        SetupCart(cart);
        SetupCatalog();

        var orderId = await handler.Handle(
            new CheckoutCartCommand(cart.Id, CartFixtures.ValidAddress(), "Leave at the door"),
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, orderId);
        Assert.Equal(CartStatus.CheckedOut, cart.Status);

        // the order snapshots what the catalog said at checkout time,
        // and both sides commit in a single SaveChanges
        _orders.Verify(
            x => x.AddAsync(
                It.Is<Order>(o =>
                    o.Id == orderId &&
                    o.CustomerId == cart.CustomerId &&
                    o.CustomerNote == "Leave at the door" &&
                    o.Currency == "USD" &&
                    o.Lines.Count == 2 &&
                    o.Lines.All(l =>
                        l.UnitPrice == 25m &&
                        l.Quantity == 1 &&
                        l.ProductName == "Test product")),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---------- Abandon ----------

    [Fact]
    public async Task Abandon_WhenCartDoesNotExist_ThrowsNotFound()
    {
        var handler = new AbandonCartCommandHandler(_carts.Object, _unitOfWork.Object);
        var cartId = Guid.NewGuid();
        _carts
            .Setup(x => x.GetByIdAsync(cartId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Cart?)null);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new AbandonCartCommand(cartId), CancellationToken.None));

        Assert.Contains("Cart", exception.Message);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Abandon_WhenCartIsNotActive_ThrowsConflict()
    {
        var handler = new AbandonCartCommandHandler(_carts.Object, _unitOfWork.Object);
        var cart = CartFixtures.CreateCart(CartStatus.Abandoned);
        SetupCart(cart);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new AbandonCartCommand(cart.Id), CancellationToken.None));

        Assert.Contains(
            "Only active carts can be abandoned (current: Abandoned).",
            exception.Message);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Abandon_WhenActive_AbandonsAndSaves()
    {
        var handler = new AbandonCartCommandHandler(_carts.Object, _unitOfWork.Object);
        var cart = CartFixtures.CreateCart(lines: 1);
        SetupCart(cart);

        await handler.Handle(new AbandonCartCommand(cart.Id), CancellationToken.None);

        Assert.Equal(CartStatus.Abandoned, cart.Status);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
