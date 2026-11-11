using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Orders.Commands.MarkOrderAsPaid;
using CommerceFlow.Application.Orders.Commands.RefundOrder;
using CommerceFlow.Application.Orders.Interfaces;
using CommerceFlow.Domain.Entities.Orders;
using CommerceFlow.Domain.Enums;
using Moq;

namespace CommerceFlow.Application.Tests.Orders;

public sealed class OrderPaymentCommandHandlerTests
{
    private readonly Mock<IOrderRepository> _orders = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private void SetupOrder(Order order) =>
        _orders
            .Setup(x => x.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

    // ---------- MarkOrderAsPaid ----------

    [Fact]
    public async Task MarkAsPaid_WhenOrderDoesNotExist_ThrowsNotFound()
    {
        var handler = new MarkOrderAsPaidCommandHandler(_orders.Object, _unitOfWork.Object);
        var orderId = Guid.NewGuid();
        _orders
            .Setup(x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new MarkOrderAsPaidCommand(orderId), CancellationToken.None));

        Assert.Contains("Order", exception.Message);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MarkAsPaid_WhenPending_MarksPaidAndStampsPaidAt()
    {
        var handler = new MarkOrderAsPaidCommandHandler(_orders.Object, _unitOfWork.Object);
        var order = OrderFixtures.CreateOrder(OrderStatus.Pending);
        SetupOrder(order);

        await handler.Handle(new MarkOrderAsPaidCommand(order.Id), CancellationToken.None);

        Assert.Equal(OrderPaymentStatus.Paid, order.PaymentStatus);
        Assert.NotNull(order.PaidAtUtc);
        Assert.Equal(OrderStatus.Pending, order.Status); // fulfillment untouched
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MarkAsPaid_WhenConfirmed_MarksPaid()
    {
        var handler = new MarkOrderAsPaidCommandHandler(_orders.Object, _unitOfWork.Object);
        var order = OrderFixtures.CreateOrder(OrderStatus.Confirmed);
        SetupOrder(order);

        await handler.Handle(new MarkOrderAsPaidCommand(order.Id), CancellationToken.None);

        Assert.Equal(OrderPaymentStatus.Paid, order.PaymentStatus);
        Assert.NotNull(order.PaidAtUtc);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MarkAsPaid_WhenAlreadyPaid_ThrowsConflict()
    {
        var handler = new MarkOrderAsPaidCommandHandler(_orders.Object, _unitOfWork.Object);
        var order = OrderFixtures.CreatePaidOrder();
        SetupOrder(order);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new MarkOrderAsPaidCommand(order.Id), CancellationToken.None));

        Assert.Contains("Order payment is already Paid", exception.Message);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MarkAsPaid_WhenAlreadyRefunded_ThrowsConflict()
    {
        var handler = new MarkOrderAsPaidCommandHandler(_orders.Object, _unitOfWork.Object);
        var order = OrderFixtures.CreatePaidOrder();
        order.Refund();
        SetupOrder(order);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new MarkOrderAsPaidCommand(order.Id), CancellationToken.None));

        Assert.Contains("Order payment is already Refunded", exception.Message);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MarkAsPaid_WhenOrderWasShipped_ThrowsConflict()
    {
        var handler = new MarkOrderAsPaidCommandHandler(_orders.Object, _unitOfWork.Object);
        var order = OrderFixtures.CreateOrder(OrderStatus.Shipped); // unpaid
        SetupOrder(order);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new MarkOrderAsPaidCommand(order.Id), CancellationToken.None));

        Assert.Contains("Only pending or confirmed orders can be marked as paid", exception.Message);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---------- RefundOrder ----------

    [Fact]
    public async Task Refund_WhenOrderDoesNotExist_ThrowsNotFound()
    {
        var handler = new RefundOrderCommandHandler(_orders.Object, _unitOfWork.Object);
        var orderId = Guid.NewGuid();
        _orders
            .Setup(x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new RefundOrderCommand(orderId), CancellationToken.None));

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Refund_WhenUnpaid_ThrowsConflict()
    {
        var handler = new RefundOrderCommandHandler(_orders.Object, _unitOfWork.Object);
        var order = OrderFixtures.CreateOrder(); // pending, unpaid
        SetupOrder(order);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new RefundOrderCommand(order.Id), CancellationToken.None));

        Assert.Contains("Only paid orders can be refunded (current: Unpaid)", exception.Message);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Refund_WhenPaid_RefundsAndStampsRefundedAtWithoutTouchingFulfillment()
    {
        var handler = new RefundOrderCommandHandler(_orders.Object, _unitOfWork.Object);
        var order = OrderFixtures.CreatePaidOrder(OrderStatus.Confirmed);
        SetupOrder(order);

        await handler.Handle(new RefundOrderCommand(order.Id), CancellationToken.None);

        Assert.Equal(OrderPaymentStatus.Refunded, order.PaymentStatus);
        Assert.NotNull(order.RefundedAtUtc);
        Assert.Equal(OrderStatus.Confirmed, order.Status); // fulfillment lifecycle untouched
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Refund_WhenOrderIsCancelledButPaid_RefundsSuccessfully()
    {
        // Independence of lifecycles: a cancelled order can still hold a refundable payment.
        var handler = new RefundOrderCommandHandler(_orders.Object, _unitOfWork.Object);
        var order = OrderFixtures.CreateCancelledPaidOrder();
        SetupOrder(order);

        await handler.Handle(new RefundOrderCommand(order.Id), CancellationToken.None);

        Assert.Equal(OrderPaymentStatus.Refunded, order.PaymentStatus);
        Assert.Equal(OrderStatus.Cancelled, order.Status); // still cancelled
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
