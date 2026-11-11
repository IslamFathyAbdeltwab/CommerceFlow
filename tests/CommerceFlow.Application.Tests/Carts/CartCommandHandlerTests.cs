using CommerceFlow.Application.Carts.Commands.AddCartItem;
using CommerceFlow.Application.Carts.Commands.ChangeCartItemQuantity;
using CommerceFlow.Application.Carts.Commands.ClearCart;
using CommerceFlow.Application.Carts.Commands.CreateCart;
using CommerceFlow.Application.Carts.Commands.RemoveCartItem;
using CommerceFlow.Application.Carts.Interfaces;
using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Domain.Entities.Carts;
using CommerceFlow.Domain.Enums;
using Moq;

namespace CommerceFlow.Application.Tests.Carts;

public sealed class CartCommandHandlerTests
{
    private readonly Mock<ICartRepository> _carts = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private void SetupCart(Cart cart) =>
        _carts
            .Setup(x => x.GetByIdAsync(cart.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

    // ---------- CreateCart ----------

    [Fact]
    public async Task CreateCart_WhenCustomerAlreadyHasActiveCart_ThrowsConflict()
    {
        var handler = new CreateCartCommandHandler(_carts.Object, _unitOfWork.Object);
        var customerId = Guid.NewGuid();
        _carts
            .Setup(x => x.GetActiveByCustomerIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CartFixtures.CreateCart());

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new CreateCartCommand(customerId), CancellationToken.None));

        Assert.Contains($"Customer '{customerId}' already has an active cart.", exception.Message);
        _carts.Verify(x => x.AddAsync(It.IsAny<Cart>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateCart_WhenCustomerHasNoActiveCart_CreatesActiveCartAndSaves()
    {
        var handler = new CreateCartCommandHandler(_carts.Object, _unitOfWork.Object);
        var customerId = Guid.NewGuid();
        _carts
            .Setup(x => x.GetActiveByCustomerIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Cart?)null);

        var id = await handler.Handle(new CreateCartCommand(customerId), CancellationToken.None);

        Assert.NotEqual(Guid.Empty, id);
        _carts.Verify(
            x => x.AddAsync(
                It.Is<Cart>(c => c.Id == id
                    && c.CustomerId == customerId
                    && c.Status == CartStatus.Active),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---------- AddCartItem ----------

    [Fact]
    public async Task AddCartItem_WhenCartDoesNotExist_ThrowsNotFound()
    {
        var handler = new AddCartItemCommandHandler(_carts.Object, _unitOfWork.Object);
        var cartId = Guid.NewGuid();
        _carts
            .Setup(x => x.GetByIdAsync(cartId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Cart?)null);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new AddCartItemCommand(cartId, Guid.NewGuid(), 1), CancellationToken.None));

        Assert.Contains("Cart", exception.Message);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddCartItem_WhenCartIsNotActive_ThrowsConflict()
    {
        var handler = new AddCartItemCommandHandler(_carts.Object, _unitOfWork.Object);
        var cart = CartFixtures.CreateCart(CartStatus.CheckedOut);
        SetupCart(cart);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new AddCartItemCommand(cart.Id, Guid.NewGuid(), 1), CancellationToken.None));

        Assert.Contains("Cart items can only be changed while the cart is active.", exception.Message);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddCartItem_WhenCartIsActive_AddsNewLineAndSaves()
    {
        var handler = new AddCartItemCommandHandler(_carts.Object, _unitOfWork.Object);
        var cart = CartFixtures.CreateCart();
        SetupCart(cart);
        var variantId = Guid.NewGuid();

        var lineId = await handler.Handle(
            new AddCartItemCommand(cart.Id, variantId, 2), CancellationToken.None);

        var line = Assert.Single(cart.Lines);
        Assert.Equal(lineId, line.Id);
        Assert.Equal(variantId, line.ProductVariantId);
        Assert.Equal(2, line.Quantity);
        Assert.Equal(1, cart.LineCount);
        Assert.Equal(2, cart.TotalQuantity);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddCartItem_WhenVariantAlreadyInCart_MergesQuantities()
    {
        var handler = new AddCartItemCommandHandler(_carts.Object, _unitOfWork.Object);
        var variantId = Guid.NewGuid();
        var cart = CartFixtures.CreateCart();
        cart.AddItem(variantId, 2);
        SetupCart(cart);

        var lineId = await handler.Handle(
            new AddCartItemCommand(cart.Id, variantId, 3), CancellationToken.None);

        var line = Assert.Single(cart.Lines);
        Assert.Equal(lineId, line.Id); // same line id -> the caller can track the merge
        Assert.Equal(5, line.Quantity); // 2 + 3
        Assert.Equal(1, cart.LineCount);
        Assert.Equal(5, cart.TotalQuantity);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddCartItem_WhenCartIsAtMaxLines_ThrowsConflict()
    {
        var handler = new AddCartItemCommandHandler(_carts.Object, _unitOfWork.Object);
        var (cart, _) = CartFixtures.ActiveCartAtMaxLines();
        SetupCart(cart);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(
                new AddCartItemCommand(cart.Id, Guid.NewGuid(), 1), CancellationToken.None));

        Assert.Contains("A cart cannot have more than 100 lines.", exception.Message);
        Assert.Equal(Cart.MaxLines, cart.LineCount); // untouched
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddCartItem_WhenCartIsAtMaxLinesButVariantIsPresent_MergesInsteadOfAddingLine()
    {
        var handler = new AddCartItemCommandHandler(_carts.Object, _unitOfWork.Object);
        var (cart, variantId) = CartFixtures.ActiveCartAtMaxLines();
        SetupCart(cart);

        var lineId = await handler.Handle(
            new AddCartItemCommand(cart.Id, variantId, 4), CancellationToken.None);

        var merged = cart.Lines.Single(l => l.ProductVariantId == variantId);
        Assert.Equal(lineId, merged.Id);
        Assert.Equal(5, merged.Quantity); // 1 + 4
        Assert.Equal(Cart.MaxLines, cart.LineCount); // no new line was created
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---------- ChangeCartItemQuantity ----------

    [Fact]
    public async Task ChangeQuantity_WhenCartDoesNotExist_ThrowsNotFound()
    {
        var handler = new ChangeCartItemQuantityCommandHandler(_carts.Object, _unitOfWork.Object);
        var cartId = Guid.NewGuid();
        _carts
            .Setup(x => x.GetByIdAsync(cartId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Cart?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new ChangeCartItemQuantityCommand(cartId, Guid.NewGuid(), 5),
                CancellationToken.None));

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ChangeQuantity_WhenCartIsNotActive_ThrowsConflict()
    {
        var handler = new ChangeCartItemQuantityCommandHandler(_carts.Object, _unitOfWork.Object);
        var cart = CartFixtures.CreateCart(CartStatus.CheckedOut);
        SetupCart(cart);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(
                new ChangeCartItemQuantityCommand(cart.Id, Guid.NewGuid(), 5),
                CancellationToken.None));

        Assert.Contains("Cart items can only be changed while the cart is active.", exception.Message);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ChangeQuantity_WhenLineDoesNotExist_ThrowsNotFound()
    {
        var handler = new ChangeCartItemQuantityCommandHandler(_carts.Object, _unitOfWork.Object);
        var (cart, _) = CartFixtures.ActiveCartWithLine();
        SetupCart(cart);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new ChangeCartItemQuantityCommand(cart.Id, Guid.NewGuid(), 5),
                CancellationToken.None));

        Assert.Contains("CartLine", exception.Message);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ChangeQuantity_WhenLineExists_UpdatesQuantityAndSaves()
    {
        var handler = new ChangeCartItemQuantityCommandHandler(_carts.Object, _unitOfWork.Object);
        var (cart, line) = CartFixtures.ActiveCartWithLine(quantity: 2);
        SetupCart(cart);

        await handler.Handle(
            new ChangeCartItemQuantityCommand(cart.Id, line.Id, 7), CancellationToken.None);

        Assert.Equal(7, line.Quantity);
        Assert.Equal(7, cart.TotalQuantity);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---------- RemoveCartItem ----------

    [Fact]
    public async Task RemoveItem_WhenCartDoesNotExist_ThrowsNotFound()
    {
        var handler = new RemoveCartItemCommandHandler(_carts.Object, _unitOfWork.Object);
        var cartId = Guid.NewGuid();
        _carts
            .Setup(x => x.GetByIdAsync(cartId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Cart?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new RemoveCartItemCommand(cartId, Guid.NewGuid()), CancellationToken.None));

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RemoveItem_WhenCartIsNotActive_ThrowsConflict()
    {
        var handler = new RemoveCartItemCommandHandler(_carts.Object, _unitOfWork.Object);
        var cart = CartFixtures.CreateCart(CartStatus.Abandoned);
        SetupCart(cart);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new RemoveCartItemCommand(cart.Id, Guid.NewGuid()), CancellationToken.None));

        Assert.Contains("Cart items can only be changed while the cart is active.", exception.Message);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RemoveItem_WhenLineDoesNotExist_ThrowsNotFound()
    {
        var handler = new RemoveCartItemCommandHandler(_carts.Object, _unitOfWork.Object);
        var (cart, _) = CartFixtures.ActiveCartWithLine();
        SetupCart(cart);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new RemoveCartItemCommand(cart.Id, Guid.NewGuid()), CancellationToken.None));

        Assert.Contains("CartLine", exception.Message);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RemoveItem_WhenLineExists_RemovesItAndSaves()
    {
        var handler = new RemoveCartItemCommandHandler(_carts.Object, _unitOfWork.Object);
        var (cart, line) = CartFixtures.ActiveCartWithLine();
        SetupCart(cart);

        await handler.Handle(new RemoveCartItemCommand(cart.Id, line.Id), CancellationToken.None);

        Assert.Empty(cart.Lines);
        Assert.Equal(0, cart.LineCount);
        Assert.Equal(0, cart.TotalQuantity);
        Assert.Equal(CartStatus.Active, cart.Status); // cart itself stays
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---------- ClearCart ----------

    [Fact]
    public async Task ClearCart_WhenCartDoesNotExist_ThrowsNotFound()
    {
        var handler = new ClearCartCommandHandler(_carts.Object, _unitOfWork.Object);
        var cartId = Guid.NewGuid();
        _carts
            .Setup(x => x.GetByIdAsync(cartId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Cart?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new ClearCartCommand(cartId), CancellationToken.None));

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ClearCart_WhenCartIsNotActive_ThrowsConflict()
    {
        var handler = new ClearCartCommandHandler(_carts.Object, _unitOfWork.Object);
        var cart = CartFixtures.CreateCart(CartStatus.CheckedOut);
        SetupCart(cart);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new ClearCartCommand(cart.Id), CancellationToken.None));

        Assert.Contains("Cart items can only be changed while the cart is active.", exception.Message);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ClearCart_WhenCartIsActiveWithLines_ClearsLinesAndSaves()
    {
        var handler = new ClearCartCommandHandler(_carts.Object, _unitOfWork.Object);
        var cart = CartFixtures.CreateCart(lines: 2);
        SetupCart(cart);

        await handler.Handle(new ClearCartCommand(cart.Id), CancellationToken.None);

        Assert.Empty(cart.Lines);
        Assert.Equal(0, cart.LineCount);
        Assert.Equal(0, cart.TotalQuantity);
        Assert.Equal(CartStatus.Active, cart.Status); // cleared, not removed
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
