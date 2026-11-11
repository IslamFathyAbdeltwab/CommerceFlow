using CommerceFlow.Application.Carts.Commands.AbandonCart;
using CommerceFlow.Application.Carts.Commands.CheckoutCart;
using CommerceFlow.Application.Carts.Interfaces;
using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Domain.Entities.Carts;
using CommerceFlow.Domain.Enums;
using Moq;

namespace CommerceFlow.Application.Tests.Carts;

public sealed class CartLifecycleCommandHandlerTests
{
    private readonly Mock<ICartRepository> _carts = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private void SetupCart(Cart cart) =>
        _carts
            .Setup(x => x.GetByIdAsync(cart.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

    // ---------- Checkout ----------

    [Fact]
    public async Task Checkout_WhenCartDoesNotExist_ThrowsNotFound()
    {
        var handler = new CheckoutCartCommandHandler(_carts.Object, _unitOfWork.Object);
        var cartId = Guid.NewGuid();
        _carts
            .Setup(x => x.GetByIdAsync(cartId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Cart?)null);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new CheckoutCartCommand(cartId), CancellationToken.None));

        Assert.Contains("Cart", exception.Message);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Checkout_WhenCartIsNotActive_ThrowsConflict()
    {
        var handler = new CheckoutCartCommandHandler(_carts.Object, _unitOfWork.Object);
        var cart = CartFixtures.CreateCart(CartStatus.CheckedOut);
        SetupCart(cart);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new CheckoutCartCommand(cart.Id), CancellationToken.None));

        Assert.Contains(
            "Only active carts can be checked out (current: CheckedOut).",
            exception.Message);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Checkout_WhenCartIsEmpty_ThrowsConflict()
    {
        var handler = new CheckoutCartCommandHandler(_carts.Object, _unitOfWork.Object);
        var cart = CartFixtures.CreateCart(); // active but no lines
        SetupCart(cart);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new CheckoutCartCommand(cart.Id), CancellationToken.None));

        Assert.Contains("Cannot check out an empty cart.", exception.Message);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Checkout_WhenActiveWithLines_ChecksOutAndSaves()
    {
        var handler = new CheckoutCartCommandHandler(_carts.Object, _unitOfWork.Object);
        var cart = CartFixtures.CreateCart(lines: 2);
        SetupCart(cart);

        await handler.Handle(new CheckoutCartCommand(cart.Id), CancellationToken.None);

        Assert.Equal(CartStatus.CheckedOut, cart.Status);
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
