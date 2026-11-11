using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Orders.Commands.AddOrderLine;
using CommerceFlow.Application.Orders.Commands.RemoveOrderLine;
using CommerceFlow.Application.Orders.Commands.UpdateOrderLineQuantity;
using CommerceFlow.Application.Orders.Interfaces;
using CommerceFlow.Domain.Entities.Orders;
using CommerceFlow.Domain.Enums;
using CommerceFlow.Domain.Exceptions;
using Moq;

namespace CommerceFlow.Application.Tests.Orders;

public sealed class OrderLineCommandHandlerTests
{
    private readonly Mock<IOrderRepository> _orders = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private void SetupOrder(Order order) =>
        _orders
            .Setup(x => x.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

    // ---------- AddOrderLine ----------

    [Fact]
    public async Task AddLine_WhenOrderDoesNotExist_ThrowsNotFound()
    {
        var handler = new AddOrderLineCommandHandler(_orders.Object, _unitOfWork.Object);
        var orderId = Guid.NewGuid();
        _orders
            .Setup(x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new AddOrderLineCommand(orderId, Guid.NewGuid(), "SKU-2", "AirPods", 50m, 3),
                CancellationToken.None));

        Assert.Contains("Order", exception.Message);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddLine_WhenOrderIsNotPending_ThrowsConflict()
    {
        var handler = new AddOrderLineCommandHandler(_orders.Object, _unitOfWork.Object);
        var order = OrderFixtures.CreateOrder(OrderStatus.Confirmed);
        SetupOrder(order);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(
                new AddOrderLineCommand(order.Id, Guid.NewGuid(), "SKU-2", "AirPods", 50m, 3),
                CancellationToken.None));

        Assert.Contains("while the order is pending", exception.Message);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddLine_WhenVariantAlreadyOnOrder_ThrowsConflict()
    {
        var handler = new AddOrderLineCommandHandler(_orders.Object, _unitOfWork.Object);
        var order = OrderFixtures.CreateOrder();
        var existingVariantId = order.Lines.Single().ProductVariantId;
        SetupOrder(order);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(
                new AddOrderLineCommand(order.Id, existingVariantId, "SKU-1", "iPhone 15", 100m, 1),
                CancellationToken.None));

        Assert.Contains("already on this order", exception.Message);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddLine_WhenPending_AddsLineRecalculatesSubtotalAndSaves()
    {
        var handler = new AddOrderLineCommandHandler(_orders.Object, _unitOfWork.Object);
        var order = OrderFixtures.CreateOrder(); // 100 x 2 = 200
        SetupOrder(order);

        await handler.Handle(
            new AddOrderLineCommand(order.Id, Guid.NewGuid(), "SKU-2", "AirPods", 50m, 3),
            CancellationToken.None);

        Assert.Equal(2, order.Lines.Count);
        Assert.Equal(350m, order.Subtotal);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---------- UpdateOrderLineQuantity ----------

    [Fact]
    public async Task UpdateLineQuantity_WhenOrderDoesNotExist_ThrowsNotFound()
    {
        var handler = new UpdateOrderLineQuantityCommandHandler(_orders.Object, _unitOfWork.Object);
        var orderId = Guid.NewGuid();
        _orders
            .Setup(x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new UpdateOrderLineQuantityCommand(orderId, Guid.NewGuid(), 5), CancellationToken.None));

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateLineQuantity_WhenOrderIsNotPending_ThrowsConflict()
    {
        var handler = new UpdateOrderLineQuantityCommandHandler(_orders.Object, _unitOfWork.Object);
        var order = OrderFixtures.CreateOrder(OrderStatus.Confirmed);
        SetupOrder(order);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(
                new UpdateOrderLineQuantityCommand(order.Id, order.Lines.Single().Id, 5),
                CancellationToken.None));

        Assert.Contains("while the order is pending", exception.Message);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateLineQuantity_WhenLineDoesNotExist_ThrowsNotFound()
    {
        var handler = new UpdateOrderLineQuantityCommandHandler(_orders.Object, _unitOfWork.Object);
        var order = OrderFixtures.CreateOrder();
        SetupOrder(order);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new UpdateOrderLineQuantityCommand(order.Id, Guid.NewGuid(), 5), CancellationToken.None));

        Assert.Contains("OrderLine", exception.Message);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateLineQuantity_WhenValid_UpdatesQuantityAndSubtotal()
    {
        var handler = new UpdateOrderLineQuantityCommandHandler(_orders.Object, _unitOfWork.Object);
        var order = OrderFixtures.CreateOrder(); // 100 x 2 = 200
        var lineId = order.Lines.Single().Id;
        SetupOrder(order);

        await handler.Handle(new UpdateOrderLineQuantityCommand(order.Id, lineId, 3), CancellationToken.None);

        Assert.Equal(3, order.Lines.Single().Quantity);
        Assert.Equal(300m, order.Subtotal);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateLineQuantity_WithZeroQuantity_ThrowsDomainException()
    {
        var handler = new UpdateOrderLineQuantityCommandHandler(_orders.Object, _unitOfWork.Object);
        var order = OrderFixtures.CreateOrder();
        var lineId = order.Lines.Single().Id;
        SetupOrder(order);

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new UpdateOrderLineQuantityCommand(order.Id, lineId, 0), CancellationToken.None));

        Assert.Equal(2, order.Lines.Single().Quantity); // unchanged
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateLineQuantity_BreakingDiscountValidity_ThrowsDomainException()
    {
        var handler = new UpdateOrderLineQuantityCommandHandler(_orders.Object, _unitOfWork.Object);
        var order = OrderFixtures.CreateOrder(); // subtotal 200
        order.ApplyDiscount(150m); // valid: 200 - 150 >= 0
        var lineId = order.Lines.Single().Id;
        SetupOrder(order);

        // New subtotal 100 - 150 discount < 0
        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new UpdateOrderLineQuantityCommand(order.Id, lineId, 1), CancellationToken.None));

        Assert.Equal(2, order.Lines.Single().Quantity); // unchanged
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---------- RemoveOrderLine ----------

    [Fact]
    public async Task RemoveLine_WhenOrderDoesNotExist_ThrowsNotFound()
    {
        var handler = new RemoveOrderLineCommandHandler(_orders.Object, _unitOfWork.Object);
        var orderId = Guid.NewGuid();
        _orders
            .Setup(x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new RemoveOrderLineCommand(orderId, Guid.NewGuid()), CancellationToken.None));

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RemoveLine_WhenOrderIsNotPending_ThrowsConflict()
    {
        var handler = new RemoveOrderLineCommandHandler(_orders.Object, _unitOfWork.Object);
        var order = OrderFixtures.CreateOrder(OrderStatus.Confirmed);
        SetupOrder(order);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new RemoveOrderLineCommand(order.Id, order.Lines.Single().Id), CancellationToken.None));

        Assert.Contains("while the order is pending", exception.Message);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RemoveLine_WhenLineDoesNotExist_ThrowsNotFound()
    {
        var handler = new RemoveOrderLineCommandHandler(_orders.Object, _unitOfWork.Object);
        var order = OrderFixtures.CreateOrder();
        SetupOrder(order);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new RemoveOrderLineCommand(order.Id, Guid.NewGuid()), CancellationToken.None));

        Assert.Contains("OrderLine", exception.Message);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RemoveLine_WhenValid_RemovesLineAndReducesSubtotal()
    {
        var handler = new RemoveOrderLineCommandHandler(_orders.Object, _unitOfWork.Object);
        var order = OrderFixtures.CreateOrder(lineCount: 2); // 2 x (100 x 2) = 400
        var lineId = order.Lines.First().Id;
        SetupOrder(order);

        await handler.Handle(new RemoveOrderLineCommand(order.Id, lineId), CancellationToken.None);

        Assert.Single(order.Lines);
        Assert.Equal(200m, order.Subtotal);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveLine_BreakingDiscountValidity_ThrowsDomainException()
    {
        var handler = new RemoveOrderLineCommandHandler(_orders.Object, _unitOfWork.Object);
        var order = OrderFixtures.CreateOrder(); // subtotal 200
        order.ApplyDiscount(150m);
        var lineId = order.Lines.Single().Id;
        SetupOrder(order);

        // Removing the only line leaves 0 - 150 < 0
        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new RemoveOrderLineCommand(order.Id, lineId), CancellationToken.None));

        Assert.Single(order.Lines); // unchanged
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
