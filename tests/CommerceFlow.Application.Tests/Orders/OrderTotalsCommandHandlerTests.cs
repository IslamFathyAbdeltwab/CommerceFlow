using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Orders.Commands.ApplyDiscount;
using CommerceFlow.Application.Orders.Commands.SetShippingFee;
using CommerceFlow.Application.Orders.Interfaces;
using CommerceFlow.Domain.Entities.Orders;
using CommerceFlow.Domain.Enums;
using CommerceFlow.Domain.Exceptions;
using Moq;

namespace CommerceFlow.Application.Tests.Orders;

public sealed class OrderTotalsCommandHandlerTests
{
    private readonly Mock<IOrderRepository> _orders = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private void SetupOrder(Order order) =>
        _orders
            .Setup(x => x.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

    // ---------- SetShippingFee ----------

    [Fact]
    public async Task SetShippingFee_WhenOrderDoesNotExist_ThrowsNotFound()
    {
        var handler = new SetShippingFeeCommandHandler(_orders.Object, _unitOfWork.Object);
        var orderId = Guid.NewGuid();
        _orders
            .Setup(x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new SetShippingFeeCommand(orderId, 25m), CancellationToken.None));

        Assert.Contains("Order", exception.Message);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SetShippingFee_WhenOrderIsNotPending_ThrowsConflict()
    {
        var handler = new SetShippingFeeCommandHandler(_orders.Object, _unitOfWork.Object);
        var order = OrderFixtures.CreateOrder(OrderStatus.Confirmed);
        SetupOrder(order);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new SetShippingFeeCommand(order.Id, 25m), CancellationToken.None));

        Assert.Contains("Totals can only be changed while the order is pending", exception.Message);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SetShippingFee_WhenNegative_ThrowsDomainException()
    {
        var handler = new SetShippingFeeCommandHandler(_orders.Object, _unitOfWork.Object);
        var order = OrderFixtures.CreateOrder();
        SetupOrder(order);

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new SetShippingFeeCommand(order.Id, -1m), CancellationToken.None));

        Assert.Contains("Shipping fee cannot be negative", exception.Message);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SetShippingFee_WhenValid_UpdatesFeeAndGrandTotal()
    {
        var handler = new SetShippingFeeCommandHandler(_orders.Object, _unitOfWork.Object);
        var order = OrderFixtures.CreateOrder(); // subtotal 200
        SetupOrder(order);

        await handler.Handle(new SetShippingFeeCommand(order.Id, 25m), CancellationToken.None);

        Assert.Equal(25m, order.ShippingFee);
        Assert.Equal(225m, order.GrandTotal);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---------- ApplyDiscount ----------

    [Fact]
    public async Task ApplyDiscount_WhenOrderDoesNotExist_ThrowsNotFound()
    {
        var handler = new ApplyDiscountCommandHandler(_orders.Object, _unitOfWork.Object);
        var orderId = Guid.NewGuid();
        _orders
            .Setup(x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new ApplyDiscountCommand(orderId, 50m), CancellationToken.None));

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ApplyDiscount_WhenOrderIsNotPending_ThrowsConflict()
    {
        var handler = new ApplyDiscountCommandHandler(_orders.Object, _unitOfWork.Object);
        var order = OrderFixtures.CreateOrder(OrderStatus.Confirmed);
        SetupOrder(order);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new ApplyDiscountCommand(order.Id, 50m), CancellationToken.None));

        Assert.Contains("Totals can only be changed while the order is pending", exception.Message);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ApplyDiscount_WhenDiscountExceedsOrderTotal_ThrowsConflict()
    {
        var handler = new ApplyDiscountCommandHandler(_orders.Object, _unitOfWork.Object);
        var order = OrderFixtures.CreateOrder(); // subtotal 200, no fee
        SetupOrder(order);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new ApplyDiscountCommand(order.Id, 201m), CancellationToken.None));

        Assert.Contains("Discount cannot exceed the order total", exception.Message);
        Assert.Equal(0m, order.DiscountAmount); // unchanged
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ApplyDiscount_WhenEqualToOrderTotal_IsAllowed()
    {
        var handler = new ApplyDiscountCommandHandler(_orders.Object, _unitOfWork.Object);
        var order = OrderFixtures.CreateOrder(); // subtotal 200
        SetupOrder(order);

        await handler.Handle(new ApplyDiscountCommand(order.Id, 200m), CancellationToken.None);

        Assert.Equal(200m, order.DiscountAmount);
        Assert.Equal(0m, order.GrandTotal); // fully discounted
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApplyDiscount_WithNegativeAmount_ThrowsDomainException()
    {
        var handler = new ApplyDiscountCommandHandler(_orders.Object, _unitOfWork.Object);
        var order = OrderFixtures.CreateOrder();
        SetupOrder(order);

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new ApplyDiscountCommand(order.Id, -5m), CancellationToken.None));

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
