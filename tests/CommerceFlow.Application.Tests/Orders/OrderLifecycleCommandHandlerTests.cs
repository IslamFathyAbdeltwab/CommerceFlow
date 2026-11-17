using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Orders.Commands.CancelOrder;
using CommerceFlow.Application.Orders.Commands.ConfirmOrder;
using CommerceFlow.Application.Orders.Commands.DeliverOrder;
using CommerceFlow.Application.Orders.Commands.ShipOrder;
using CommerceFlow.Application.Orders.Events;
using CommerceFlow.Application.Orders.Interfaces;
using CommerceFlow.Domain.Entities.Orders;
using CommerceFlow.Domain.Enums;
using MediatR;
using Moq;

namespace CommerceFlow.Application.Tests.Orders;

public sealed class OrderLifecycleCommandHandlerTests
{
    private readonly Mock<IOrderRepository> _orders = new();
    private readonly Mock<IMediator> _mediator = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private void SetupOrder(Order order) =>
        _orders
            .Setup(x => x.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

    // ---------- Confirm ----------

    [Fact]
    public async Task Confirm_WhenOrderDoesNotExist_ThrowsNotFound()
    {
        var handler = new ConfirmOrderCommandHandler(_orders.Object, _mediator.Object, _unitOfWork.Object);
        var orderId = Guid.NewGuid();
        _orders
            .Setup(x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new ConfirmOrderCommand(orderId), CancellationToken.None));

        Assert.Contains("Order", exception.Message);
        _mediator.Verify(
            x => x.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Confirm_WhenOrderIsAlreadyConfirmed_ThrowsConflict()
    {
        var handler = new ConfirmOrderCommandHandler(_orders.Object, _mediator.Object, _unitOfWork.Object);
        var order = OrderFixtures.CreateOrder(OrderStatus.Confirmed);
        SetupOrder(order);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new ConfirmOrderCommand(order.Id), CancellationToken.None));

        Assert.Contains("Only pending orders can be confirmed (current: Confirmed)", exception.Message);
        _mediator.Verify(
            x => x.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Confirm_WithoutLines_ThrowsConflict()
    {
        var handler = new ConfirmOrderCommandHandler(_orders.Object, _mediator.Object, _unitOfWork.Object);
        var order = Order.Create(OrderFixtures.CustomerId, OrderFixtures.CreateAddress()); // no lines
        SetupOrder(order);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new ConfirmOrderCommand(order.Id), CancellationToken.None));

        Assert.Contains("Cannot confirm an order without lines", exception.Message);
        _mediator.Verify(
            x => x.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Confirm_WhenPendingWithLines_ConfirmsAndSaves()
    {
        var handler = new ConfirmOrderCommandHandler(_orders.Object, _mediator.Object, _unitOfWork.Object);
        var order = OrderFixtures.CreateOrder();
        SetupOrder(order);

        await handler.Handle(new ConfirmOrderCommand(order.Id), CancellationToken.None);

        Assert.Equal(OrderStatus.Confirmed, order.Status);
        _mediator.Verify(
            x => x.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Confirm_PublishesOrderConfirmedEventBeforeSaving()
    {
        var handler = new ConfirmOrderCommandHandler(_orders.Object, _mediator.Object, _unitOfWork.Object);
        var order = OrderFixtures.CreateOrder();
        SetupOrder(order);

        // record the exact sequence: the event is published while the order
        // change is still uncommitted, so subscribers share this unit of work
        var calls = new List<string>();
        _mediator
            .Setup(x => x.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()))
            .Callback<INotification, CancellationToken>((_, _) => calls.Add("publish"))
            .Returns(Task.CompletedTask);
        _unitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => calls.Add("save"))
            .ReturnsAsync(1);

        await handler.Handle(new ConfirmOrderCommand(order.Id), CancellationToken.None);

        _mediator.Verify(
            x => x.Publish(
                It.Is<OrderConfirmedIntegrationEvent>(e =>
                    e.OrderId == order.Id &&
                    e.CustomerId == order.CustomerId &&
                    e.OrderNumber == order.OrderNumber &&
                    e.GrandTotal == order.GrandTotal &&
                    e.Currency == order.Currency),
                It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.Equal(new[] { "publish", "save" }, calls);
    }

    // ---------- Ship ----------

    [Fact]
    public async Task Ship_WhenOrderDoesNotExist_ThrowsNotFound()
    {
        var handler = new ShipOrderCommandHandler(_orders.Object, _unitOfWork.Object);
        var orderId = Guid.NewGuid();
        _orders
            .Setup(x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new ShipOrderCommand(orderId), CancellationToken.None));

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Ship_WhenOrderIsStillPending_ThrowsConflict()
    {
        var handler = new ShipOrderCommandHandler(_orders.Object, _unitOfWork.Object);
        var order = OrderFixtures.CreateOrder(OrderStatus.Pending);
        SetupOrder(order);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new ShipOrderCommand(order.Id), CancellationToken.None));

        Assert.Contains("Only confirmed orders can be shipped (current: Pending)", exception.Message);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Ship_WhenConfirmed_ShipsAndStampsShippedAt()
    {
        var handler = new ShipOrderCommandHandler(_orders.Object, _unitOfWork.Object);
        var order = OrderFixtures.CreateOrder(OrderStatus.Confirmed);
        SetupOrder(order);

        await handler.Handle(new ShipOrderCommand(order.Id), CancellationToken.None);

        Assert.Equal(OrderStatus.Shipped, order.Status);
        Assert.NotNull(order.ShippedAtUtc);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---------- Deliver ----------

    [Fact]
    public async Task Deliver_WhenOrderDoesNotExist_ThrowsNotFound()
    {
        var handler = new DeliverOrderCommandHandler(_orders.Object, _unitOfWork.Object);
        var orderId = Guid.NewGuid();
        _orders
            .Setup(x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new DeliverOrderCommand(orderId), CancellationToken.None));

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Deliver_WhenOrderIsOnlyConfirmed_ThrowsConflict()
    {
        var handler = new DeliverOrderCommandHandler(_orders.Object, _unitOfWork.Object);
        var order = OrderFixtures.CreateOrder(OrderStatus.Confirmed);
        SetupOrder(order);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new DeliverOrderCommand(order.Id), CancellationToken.None));

        Assert.Contains("Only shipped orders can be delivered (current: Confirmed)", exception.Message);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Deliver_WhenShipped_DeliversAndStampsDeliveredAt()
    {
        var handler = new DeliverOrderCommandHandler(_orders.Object, _unitOfWork.Object);
        var order = OrderFixtures.CreateOrder(OrderStatus.Shipped);
        SetupOrder(order);

        await handler.Handle(new DeliverOrderCommand(order.Id), CancellationToken.None);

        Assert.Equal(OrderStatus.Delivered, order.Status);
        Assert.NotNull(order.DeliveredAtUtc);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---------- Cancel ----------

    [Fact]
    public async Task Cancel_WhenOrderDoesNotExist_ThrowsNotFound()
    {
        var handler = new CancelOrderCommandHandler(_orders.Object, _unitOfWork.Object);
        var orderId = Guid.NewGuid();
        _orders
            .Setup(x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new CancelOrderCommand(orderId, "reason"), CancellationToken.None));

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Cancel_WhenOrderWasShipped_ThrowsConflict()
    {
        var handler = new CancelOrderCommandHandler(_orders.Object, _unitOfWork.Object);
        var order = OrderFixtures.CreateOrder(OrderStatus.Shipped);
        SetupOrder(order);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new CancelOrderCommand(order.Id, "too late"), CancellationToken.None));

        Assert.Contains(
            "Only pending or confirmed orders can be cancelled (current: Shipped)",
            exception.Message);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Cancel_WhenPendingWithReason_CancelsAndStampsReasonAndTimestamp()
    {
        var handler = new CancelOrderCommandHandler(_orders.Object, _unitOfWork.Object);
        var order = OrderFixtures.CreateOrder(OrderStatus.Pending);
        SetupOrder(order);

        await handler.Handle(new CancelOrderCommand(order.Id, "  Customer changed their mind  "), CancellationToken.None);

        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal("Customer changed their mind", order.CancelReason); // trimmed
        Assert.NotNull(order.CancelledAtUtc);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Cancel_WhenConfirmedWithoutReason_StoresNullReason()
    {
        var handler = new CancelOrderCommandHandler(_orders.Object, _unitOfWork.Object);
        var order = OrderFixtures.CreateOrder(OrderStatus.Confirmed);
        SetupOrder(order);

        await handler.Handle(new CancelOrderCommand(order.Id), CancellationToken.None);

        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Null(order.CancelReason);
        Assert.NotNull(order.CancelledAtUtc);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
