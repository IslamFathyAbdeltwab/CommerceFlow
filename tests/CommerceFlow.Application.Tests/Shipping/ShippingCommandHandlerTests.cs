using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Orders.Interfaces;
using CommerceFlow.Application.Shipping.Commands.CancelShipment;
using CommerceFlow.Application.Shipping.Commands.CreateShipment;
using CommerceFlow.Application.Shipping.Commands.DeliverShipment;
using CommerceFlow.Application.Shipping.Commands.FailShipment;
using CommerceFlow.Application.Shipping.Commands.MarkShipmentInTransit;
using CommerceFlow.Application.Shipping.Commands.ReturnShipment;
using CommerceFlow.Application.Shipping.Commands.ShipShipment;
using CommerceFlow.Application.Shipping.Interfaces;
using CommerceFlow.Application.Tests.Orders;
using CommerceFlow.Domain.Entities.Orders;
using CommerceFlow.Domain.Entities.Shipping;
using CommerceFlow.Domain.Enums;
using CommerceFlow.Domain.Exceptions;
using Moq;

namespace CommerceFlow.Application.Tests.Shipping;

public sealed class ShippingCommandHandlerTests
{
    private readonly Mock<IShipmentRepository> _shipments = new();
    private readonly Mock<IOrderRepository> _orders = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private void SetupOrder(Guid orderId, Order order) =>
        _orders
            .Setup(x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

    private void SetupOrderMissing(Guid orderId) =>
        _orders
            .Setup(x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

    private void SetupShipment(Shipment shipment) =>
        _shipments
            .Setup(x => x.GetByIdAsync(shipment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shipment);

    private void SetupTrackingFree() =>
        _shipments
            .Setup(x => x.GetByTrackingNumberAsync(
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Shipment?)null);

    private void SetupTrackingUsed(string trackingNumber, Shipment taken) =>
        _shipments
            .Setup(x => x.GetByTrackingNumberAsync(
                trackingNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(taken);

    // every create test that reaches the quantity pre-check needs this:
    // loose Moq would hand the handler a null list
    private void SetupExistingShipments(Guid orderId, params Shipment[] existing) =>
        _shipments
            .Setup(x => x.GetByOrderIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing.ToList());

    private static void VerifyNeverSaved(Mock<IUnitOfWork> unitOfWork) =>
        unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

    // ---------- CreateShipment ----------

    [Fact]
    public async Task Create_WithValidInput_CreatesPendingShipmentAndSaves()
    {
        var handler = new CreateShipmentCommandHandler(
            _shipments.Object, _orders.Object, _unitOfWork.Object);
        var order = OrderFixtures.CreateOrder(OrderStatus.Confirmed); // line qty 2
        var variantId = order.Lines.First().ProductVariantId;
        SetupOrder(order.Id, order);
        SetupTrackingFree();
        SetupExistingShipments(order.Id);

        var id = await handler.Handle(
            ShippingFixtures.ValidCreateCommand(
                order.Id,
                variantId,
                quantity: 2,
                carrier: " DHL ",
                trackingNumber: "  TRK-9  ",
                serviceLevel: " Express "),
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, id);
        _shipments.Verify(
            x => x.AddAsync(
                It.Is<Shipment>(s => s.Id == id
                    && s.OrderId == order.Id
                    && s.Status == ShipmentStatus.Pending
                    && s.Carrier == "DHL" // trimmed
                    && s.TrackingNumber == "TRK-9" // trimmed before the lookup
                    && s.ServiceLevel == "Express" // trimmed
                    && s.Items.Count == 1
                    && s.Items.Single().ProductVariantId == variantId
                    && s.Items.Single().Quantity == 2),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _shipments.Verify(
            x => x.GetByTrackingNumberAsync("TRK-9", It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_WhenOrderAlreadyShipped_IsStillAllowed()
    {
        var handler = new CreateShipmentCommandHandler(
            _shipments.Object, _orders.Object, _unitOfWork.Object);
        var order = OrderFixtures.CreateOrder(OrderStatus.Shipped);
        SetupOrder(order.Id, order);
        SetupTrackingFree();
        SetupExistingShipments(order.Id);

        var id = await handler.Handle(
            ShippingFixtures.ValidCreateCommand(
                order.Id, order.Lines.First().ProductVariantId),
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, id);
        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_WhenOrderDoesNotExist_ThrowsNotFound()
    {
        var handler = new CreateShipmentCommandHandler(
            _shipments.Object, _orders.Object, _unitOfWork.Object);
        var orderId = Guid.NewGuid();
        SetupOrderMissing(orderId);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                ShippingFixtures.ValidCreateCommand(orderId),
                CancellationToken.None));

        _shipments.Verify(
            x => x.AddAsync(It.IsAny<Shipment>(), It.IsAny<CancellationToken>()), Times.Never);
        VerifyNeverSaved(_unitOfWork);
    }

    [Theory]
    [InlineData(OrderStatus.Pending)]
    [InlineData(OrderStatus.Delivered)]
    [InlineData(OrderStatus.Cancelled)]
    public async Task Create_WhenOrderIsNotShippable_ThrowsConflict(OrderStatus status)
    {
        var handler = new CreateShipmentCommandHandler(
            _shipments.Object, _orders.Object, _unitOfWork.Object);
        var order = OrderFixtures.CreateOrder(status);
        SetupOrder(order.Id, order);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(
                ShippingFixtures.ValidCreateCommand(order.Id),
                CancellationToken.None));

        Assert.Contains(
            $"Only confirmed orders can be shipped (current: {status}).", exception.Message);
        _shipments.Verify(
            x => x.GetByTrackingNumberAsync(
                It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _shipments.Verify(
            x => x.AddAsync(It.IsAny<Shipment>(), It.IsAny<CancellationToken>()), Times.Never);
        VerifyNeverSaved(_unitOfWork);
    }

    [Fact]
    public async Task Create_WhenTrackingNumberIsAlreadyUsed_ThrowsConflict()
    {
        var handler = new CreateShipmentCommandHandler(
            _shipments.Object, _orders.Object, _unitOfWork.Object);
        var order = OrderFixtures.CreateOrder(OrderStatus.Confirmed);
        SetupOrder(order.Id, order);
        SetupTrackingUsed("TRACK-001", ShippingFixtures.CreateShipment());

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(
                ShippingFixtures.ValidCreateCommand(order.Id),
                CancellationToken.None));

        Assert.Contains("Tracking number is already in use.", exception.Message);
        _shipments.Verify(
            x => x.AddAsync(It.IsAny<Shipment>(), It.IsAny<CancellationToken>()), Times.Never);
        VerifyNeverSaved(_unitOfWork);
    }

    [Fact]
    public async Task Create_WhenItemDoesNotMatchAnyOrderLine_ThrowsConflict()
    {
        var handler = new CreateShipmentCommandHandler(
            _shipments.Object, _orders.Object, _unitOfWork.Object);
        var order = OrderFixtures.CreateOrder(OrderStatus.Confirmed);
        SetupOrder(order.Id, order);
        SetupTrackingFree();
        SetupExistingShipments(order.Id);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(
                ShippingFixtures.ValidCreateCommand(order.Id, Guid.NewGuid()),
                CancellationToken.None));

        Assert.Contains("Shipment item does not match any order line.", exception.Message);
        _shipments.Verify(
            x => x.AddAsync(It.IsAny<Shipment>(), It.IsAny<CancellationToken>()), Times.Never);
        VerifyNeverSaved(_unitOfWork);
    }

    [Fact]
    public async Task Create_WhenQuantityExceedsWhatWasOrdered_ThrowsConflict()
    {
        var handler = new CreateShipmentCommandHandler(
            _shipments.Object, _orders.Object, _unitOfWork.Object);
        var order = OrderFixtures.CreateOrder(OrderStatus.Confirmed); // line qty 2
        var variantId = order.Lines.First().ProductVariantId;
        SetupOrder(order.Id, order);
        SetupTrackingFree();
        SetupExistingShipments(order.Id);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(
                ShippingFixtures.ValidCreateCommand(order.Id, variantId, quantity: 3),
                CancellationToken.None));

        Assert.Contains("Shipment quantity exceeds the unshipped quantity.", exception.Message);
        _shipments.Verify(
            x => x.AddAsync(It.IsAny<Shipment>(), It.IsAny<CancellationToken>()), Times.Never);
        VerifyNeverSaved(_unitOfWork);
    }

    // Only Cancelled/Returned boxes release their claim on the quantity -
    // every other state still holds what it carries.
    [Theory]
    [InlineData(ShipmentStatus.Pending)]
    [InlineData(ShipmentStatus.Shipped)]
    [InlineData(ShipmentStatus.InTransit)]
    [InlineData(ShipmentStatus.Failed)]
    [InlineData(ShipmentStatus.Delivered)]
    public async Task Create_WhenActiveBoxStillHoldsTheQuantity_ThrowsConflict(
        ShipmentStatus boxStatus)
    {
        var handler = new CreateShipmentCommandHandler(
            _shipments.Object, _orders.Object, _unitOfWork.Object);
        var order = OrderFixtures.CreateOrder(OrderStatus.Confirmed); // line qty 2
        var variantId = order.Lines.First().ProductVariantId;
        SetupOrder(order.Id, order);
        SetupTrackingFree();
        SetupExistingShipments(order.Id,
            ShippingFixtures.CreateShipment(
                orderId: order.Id,
                status: boxStatus,
                productVariantId: variantId,
                quantity: 2)); // the box already holds the full ordered qty

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(
                ShippingFixtures.ValidCreateCommand(order.Id, variantId, quantity: 1),
                CancellationToken.None));

        Assert.Contains("Shipment quantity exceeds the unshipped quantity.", exception.Message);
        _shipments.Verify(
            x => x.AddAsync(It.IsAny<Shipment>(), It.IsAny<CancellationToken>()), Times.Never);
        VerifyNeverSaved(_unitOfWork);
    }

    [Theory]
    [InlineData(ShipmentStatus.Cancelled)]
    [InlineData(ShipmentStatus.Returned)]
    public async Task Create_WhenPreviousBoxWasCancelledOrReturned_ReleasesQuantityClaim(
        ShipmentStatus releasedStatus)
    {
        var handler = new CreateShipmentCommandHandler(
            _shipments.Object, _orders.Object, _unitOfWork.Object);
        var order = OrderFixtures.CreateOrder(OrderStatus.Confirmed); // line qty 2
        var variantId = order.Lines.First().ProductVariantId;
        SetupOrder(order.Id, order);
        SetupTrackingFree();
        SetupExistingShipments(order.Id,
            ShippingFixtures.CreateShipment(
                orderId: order.Id,
                status: releasedStatus,
                productVariantId: variantId,
                quantity: 2)); // claim released - the full qty is available again

        var id = await handler.Handle(
            ShippingFixtures.ValidCreateCommand(order.Id, variantId, quantity: 2),
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, id);
        _shipments.Verify(
            x => x.AddAsync(It.IsAny<Shipment>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---------- ShipShipment ----------

    [Fact]
    public async Task Ship_WithPendingShipment_MarksShippedAndSyncsOrder()
    {
        var handler = new ShipShipmentCommandHandler(
            _shipments.Object, _orders.Object, _unitOfWork.Object);
        var order = OrderFixtures.CreateOrder(OrderStatus.Confirmed);
        SetupOrder(order.Id, order);
        var shipment = ShippingFixtures.CreateShipment(orderId: order.Id);
        SetupShipment(shipment);

        await handler.Handle(new ShipShipmentCommand(shipment.Id), CancellationToken.None);

        Assert.Equal(ShipmentStatus.Shipped, shipment.Status);
        Assert.NotNull(shipment.ShippedAtUtc);
        Assert.Equal(OrderStatus.Shipped, order.Status); // first box hands the order over
        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Ship_WhenOrderWasAlreadyHandedOver_LeavesOrderAlone()
    {
        var handler = new ShipShipmentCommandHandler(
            _shipments.Object, _orders.Object, _unitOfWork.Object);
        var order = OrderFixtures.CreateOrder(OrderStatus.Shipped); // second partial box
        SetupOrder(order.Id, order);
        var shipment = ShippingFixtures.CreateShipment(orderId: order.Id);
        SetupShipment(shipment);

        await handler.Handle(new ShipShipmentCommand(shipment.Id), CancellationToken.None);

        Assert.Equal(ShipmentStatus.Shipped, shipment.Status);
        Assert.Equal(OrderStatus.Shipped, order.Status); // unchanged, no re-hand-off
        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Ship_WhenShipmentDoesNotExist_ThrowsNotFound()
    {
        var handler = new ShipShipmentCommandHandler(
            _shipments.Object, _orders.Object, _unitOfWork.Object);
        _shipments
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Shipment?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new ShipShipmentCommand(Guid.NewGuid()), CancellationToken.None));

        _orders.Verify(
            x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        VerifyNeverSaved(_unitOfWork);
    }

    [Theory]
    [InlineData(ShipmentStatus.Shipped)]
    [InlineData(ShipmentStatus.InTransit)]
    [InlineData(ShipmentStatus.Delivered)]
    [InlineData(ShipmentStatus.Failed)]
    [InlineData(ShipmentStatus.Returned)]
    [InlineData(ShipmentStatus.Cancelled)]
    public async Task Ship_WhenShipmentIsNotPending_ThrowsConflict(ShipmentStatus status)
    {
        var handler = new ShipShipmentCommandHandler(
            _shipments.Object, _orders.Object, _unitOfWork.Object);
        var shipment = ShippingFixtures.CreateShipment(status: status);
        SetupShipment(shipment);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new ShipShipmentCommand(shipment.Id), CancellationToken.None));

        Assert.Contains(
            $"Only pending shipments can be shipped (current: {status}).", exception.Message);
        _orders.Verify(
            x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        VerifyNeverSaved(_unitOfWork);
    }

    [Fact]
    public async Task Ship_WhenOrderDoesNotExist_ThrowsNotFound()
    {
        var handler = new ShipShipmentCommandHandler(
            _shipments.Object, _orders.Object, _unitOfWork.Object);
        var shipment = ShippingFixtures.CreateShipment(orderId: Guid.NewGuid());
        SetupShipment(shipment);
        SetupOrderMissing(shipment.OrderId);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new ShipShipmentCommand(shipment.Id), CancellationToken.None));

        Assert.Equal(ShipmentStatus.Pending, shipment.Status); // nothing moved
        VerifyNeverSaved(_unitOfWork);
    }

    [Fact]
    public async Task Ship_WhenOrderIsNotConfirmed_ThrowsConflict()
    {
        var handler = new ShipShipmentCommandHandler(
            _shipments.Object, _orders.Object, _unitOfWork.Object);
        var order = OrderFixtures.CreateOrder(OrderStatus.Pending);
        SetupOrder(order.Id, order);
        var shipment = ShippingFixtures.CreateShipment(orderId: order.Id);
        SetupShipment(shipment);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new ShipShipmentCommand(shipment.Id), CancellationToken.None));

        Assert.Contains(
            $"Only confirmed orders can be shipped (current: {OrderStatus.Pending}).",
            exception.Message);
        Assert.Equal(ShipmentStatus.Pending, shipment.Status); // nothing moved
        VerifyNeverSaved(_unitOfWork);
    }

    [Fact]
    public async Task Ship_WhenBoxIsEmpty_ThrowsDomainExceptionWithoutSaving()
    {
        var handler = new ShipShipmentCommandHandler(
            _shipments.Object, _orders.Object, _unitOfWork.Object);
        var order = OrderFixtures.CreateOrder(OrderStatus.Confirmed);
        SetupOrder(order.Id, order);
        var shipment = ShippingFixtures.CreateShipment(
            orderId: order.Id, withItem: false);
        SetupShipment(shipment);

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new ShipShipmentCommand(shipment.Id), CancellationToken.None));

        Assert.Contains("at least one item", exception.Message);
        Assert.Equal(ShipmentStatus.Pending, shipment.Status);
        VerifyNeverSaved(_unitOfWork);
    }

    // ---------- MarkShipmentInTransit ----------

    [Fact]
    public async Task InTransit_FromShipped_MarksInTransitAndSaves()
    {
        var handler = new MarkShipmentInTransitCommandHandler(_shipments.Object, _unitOfWork.Object);
        var shipment = ShippingFixtures.CreateShipment(status: ShipmentStatus.Shipped);
        SetupShipment(shipment);

        await handler.Handle(
            new MarkShipmentInTransitCommand(shipment.Id), CancellationToken.None);

        Assert.Equal(ShipmentStatus.InTransit, shipment.Status);
        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InTransit_WhenShipmentDoesNotExist_ThrowsNotFound()
    {
        var handler = new MarkShipmentInTransitCommandHandler(_shipments.Object, _unitOfWork.Object);
        _shipments
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Shipment?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new MarkShipmentInTransitCommand(Guid.NewGuid()), CancellationToken.None));

        VerifyNeverSaved(_unitOfWork);
    }

    [Theory]
    [InlineData(ShipmentStatus.Pending)]
    [InlineData(ShipmentStatus.InTransit)]
    [InlineData(ShipmentStatus.Delivered)]
    [InlineData(ShipmentStatus.Failed)]
    [InlineData(ShipmentStatus.Returned)]
    [InlineData(ShipmentStatus.Cancelled)]
    public async Task InTransit_WhenShipmentIsNotShipped_ThrowsConflict(ShipmentStatus status)
    {
        var handler = new MarkShipmentInTransitCommandHandler(_shipments.Object, _unitOfWork.Object);
        var shipment = ShippingFixtures.CreateShipment(status: status);
        SetupShipment(shipment);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(
                new MarkShipmentInTransitCommand(shipment.Id), CancellationToken.None));

        Assert.Contains(
            $"Only shipped shipments can be marked in transit (current: {status}).",
            exception.Message);
        Assert.Equal(status, shipment.Status); // nothing moved
        VerifyNeverSaved(_unitOfWork);
    }

    // ---------- DeliverShipment ----------

    [Fact]
    public async Task Deliver_FromShipped_MarksDeliveredAndSyncsOrder()
    {
        var handler = new DeliverShipmentCommandHandler(
            _shipments.Object, _orders.Object, _unitOfWork.Object);
        var order = OrderFixtures.CreateOrder(OrderStatus.Shipped);
        SetupOrder(order.Id, order);
        var shipment = ShippingFixtures.CreateShipment(
            orderId: order.Id, status: ShipmentStatus.Shipped);
        SetupShipment(shipment);

        await handler.Handle(new DeliverShipmentCommand(shipment.Id), CancellationToken.None);

        Assert.Equal(ShipmentStatus.Delivered, shipment.Status);
        Assert.NotNull(shipment.DeliveredAtUtc);
        Assert.Equal(OrderStatus.Delivered, order.Status); // syncs forward
        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Deliver_FromInTransit_MarksDelivered()
    {
        var handler = new DeliverShipmentCommandHandler(
            _shipments.Object, _orders.Object, _unitOfWork.Object);
        var order = OrderFixtures.CreateOrder(OrderStatus.Shipped);
        SetupOrder(order.Id, order);
        var shipment = ShippingFixtures.CreateShipment(
            orderId: order.Id, status: ShipmentStatus.InTransit);
        SetupShipment(shipment);

        await handler.Handle(new DeliverShipmentCommand(shipment.Id), CancellationToken.None);

        Assert.Equal(ShipmentStatus.Delivered, shipment.Status);
        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(ShipmentStatus.Pending)]
    [InlineData(ShipmentStatus.Delivered)]
    [InlineData(ShipmentStatus.Failed)]
    [InlineData(ShipmentStatus.Returned)]
    [InlineData(ShipmentStatus.Cancelled)]
    public async Task Deliver_WhenShipmentIsNotShippedOrInTransit_ThrowsConflict(
        ShipmentStatus status)
    {
        var handler = new DeliverShipmentCommandHandler(
            _shipments.Object, _orders.Object, _unitOfWork.Object);
        var shipment = ShippingFixtures.CreateShipment(status: status);
        SetupShipment(shipment);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new DeliverShipmentCommand(shipment.Id), CancellationToken.None));

        Assert.Contains(
            $"Only shipped or in-transit shipments can be delivered (current: {status}).",
            exception.Message);
        _orders.Verify(
            x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        VerifyNeverSaved(_unitOfWork);
    }

    [Fact]
    public async Task Deliver_WhenOrderDoesNotExist_ThrowsNotFound()
    {
        var handler = new DeliverShipmentCommandHandler(
            _shipments.Object, _orders.Object, _unitOfWork.Object);
        var shipment = ShippingFixtures.CreateShipment(status: ShipmentStatus.Shipped);
        SetupShipment(shipment);
        SetupOrderMissing(shipment.OrderId);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new DeliverShipmentCommand(shipment.Id), CancellationToken.None));

        Assert.Equal(ShipmentStatus.Shipped, shipment.Status); // nothing moved
        VerifyNeverSaved(_unitOfWork);
    }

    [Fact]
    public async Task Deliver_WhenOrderWasNeverHandedOver_RecordsCarrierFactOnly()
    {
        var handler = new DeliverShipmentCommandHandler(
            _shipments.Object, _orders.Object, _unitOfWork.Object);
        var order = OrderFixtures.CreateOrder(OrderStatus.Confirmed); // first box not shipped yet
        SetupOrder(order.Id, order);
        var shipment = ShippingFixtures.CreateShipment(
            orderId: order.Id, status: ShipmentStatus.Shipped);
        SetupShipment(shipment);

        await handler.Handle(new DeliverShipmentCommand(shipment.Id), CancellationToken.None);

        Assert.Equal(ShipmentStatus.Delivered, shipment.Status); // fact recorded...
        Assert.Equal(OrderStatus.Confirmed, order.Status); // ...order untouched
        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Deliver_WhenSecondBoxArrivesAfterOrderDelivered_LeavesOrderAlone()
    {
        var handler = new DeliverShipmentCommandHandler(
            _shipments.Object, _orders.Object, _unitOfWork.Object);
        var order = OrderFixtures.CreateOrder(OrderStatus.Delivered);
        SetupOrder(order.Id, order);
        var shipment = ShippingFixtures.CreateShipment(
            orderId: order.Id, status: ShipmentStatus.InTransit);
        SetupShipment(shipment);

        await handler.Handle(new DeliverShipmentCommand(shipment.Id), CancellationToken.None);

        Assert.Equal(ShipmentStatus.Delivered, shipment.Status);
        Assert.Equal(OrderStatus.Delivered, order.Status); // no re-delivery
        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---------- FailShipment ----------

    [Fact]
    public async Task Fail_FromShipped_MarksFailedWithTrimmedReason()
    {
        var handler = new FailShipmentCommandHandler(_shipments.Object, _unitOfWork.Object);
        var shipment = ShippingFixtures.CreateShipment(status: ShipmentStatus.Shipped);
        SetupShipment(shipment);

        await handler.Handle(
            new FailShipmentCommand(shipment.Id, "  Nobody home.  "), CancellationToken.None);

        Assert.Equal(ShipmentStatus.Failed, shipment.Status);
        Assert.Equal("Nobody home.", shipment.FailureReason); // trimmed
        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Fail_FromInTransit_MarksFailed()
    {
        var handler = new FailShipmentCommandHandler(_shipments.Object, _unitOfWork.Object);
        var shipment = ShippingFixtures.CreateShipment(status: ShipmentStatus.InTransit);
        SetupShipment(shipment);

        await handler.Handle(
            new FailShipmentCommand(shipment.Id, "Bad address."), CancellationToken.None);

        Assert.Equal(ShipmentStatus.Failed, shipment.Status);
        Assert.Equal("Bad address.", shipment.FailureReason);
        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Fail_WhenShipmentDoesNotExist_ThrowsNotFound()
    {
        var handler = new FailShipmentCommandHandler(_shipments.Object, _unitOfWork.Object);
        _shipments
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Shipment?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new FailShipmentCommand(Guid.NewGuid(), "Nobody home."), CancellationToken.None));

        VerifyNeverSaved(_unitOfWork);
    }

    [Theory]
    [InlineData(ShipmentStatus.Pending)]
    [InlineData(ShipmentStatus.Failed)]
    [InlineData(ShipmentStatus.Delivered)]
    [InlineData(ShipmentStatus.Returned)]
    [InlineData(ShipmentStatus.Cancelled)]
    public async Task Fail_WhenShipmentIsNotShippedOrInTransit_ThrowsConflict(
        ShipmentStatus status)
    {
        var handler = new FailShipmentCommandHandler(_shipments.Object, _unitOfWork.Object);
        var shipment = ShippingFixtures.CreateShipment(status: status);
        SetupShipment(shipment);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(
                new FailShipmentCommand(shipment.Id, "Nobody home."), CancellationToken.None));

        Assert.Contains(
            $"Only shipped or in-transit shipments can be marked as failed (current: {status}).",
            exception.Message);
        Assert.Equal(status, shipment.Status); // original reason/status untouched
        VerifyNeverSaved(_unitOfWork);
    }

    // ---------- ReturnShipment ----------

    [Fact]
    public async Task Return_FromFailed_MarksReturnedAndSaves()
    {
        var handler = new ReturnShipmentCommandHandler(_shipments.Object, _unitOfWork.Object);
        var shipment = ShippingFixtures.CreateShipment(
            status: ShipmentStatus.Failed, failureReason: "Nobody home.");
        SetupShipment(shipment);

        await handler.Handle(new ReturnShipmentCommand(shipment.Id), CancellationToken.None);

        Assert.Equal(ShipmentStatus.Returned, shipment.Status);
        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Return_WhenShipmentDoesNotExist_ThrowsNotFound()
    {
        var handler = new ReturnShipmentCommandHandler(_shipments.Object, _unitOfWork.Object);
        _shipments
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Shipment?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new ReturnShipmentCommand(Guid.NewGuid()), CancellationToken.None));

        VerifyNeverSaved(_unitOfWork);
    }

    [Theory]
    [InlineData(ShipmentStatus.Pending)]
    [InlineData(ShipmentStatus.Shipped)]
    [InlineData(ShipmentStatus.InTransit)]
    [InlineData(ShipmentStatus.Delivered)]
    [InlineData(ShipmentStatus.Returned)]
    [InlineData(ShipmentStatus.Cancelled)]
    public async Task Return_WhenShipmentIsNotFailed_ThrowsConflict(ShipmentStatus status)
    {
        var handler = new ReturnShipmentCommandHandler(_shipments.Object, _unitOfWork.Object);
        var shipment = ShippingFixtures.CreateShipment(status: status);
        SetupShipment(shipment);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new ReturnShipmentCommand(shipment.Id), CancellationToken.None));

        Assert.Contains(
            $"Only failed shipments can be returned to sender (current: {status}).",
            exception.Message);
        Assert.Equal(status, shipment.Status);
        VerifyNeverSaved(_unitOfWork);
    }

    // ---------- CancelShipment ----------

    [Fact]
    public async Task Cancel_WithPendingShipment_CancelsAndStampsTimestamp()
    {
        var handler = new CancelShipmentCommandHandler(_shipments.Object, _unitOfWork.Object);
        var shipment = ShippingFixtures.CreateShipment();
        SetupShipment(shipment);

        await handler.Handle(new CancelShipmentCommand(shipment.Id), CancellationToken.None);

        Assert.Equal(ShipmentStatus.Cancelled, shipment.Status);
        Assert.NotNull(shipment.CancelledAtUtc);
        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Cancel_WhenShipmentDoesNotExist_ThrowsNotFound()
    {
        var handler = new CancelShipmentCommandHandler(_shipments.Object, _unitOfWork.Object);
        _shipments
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Shipment?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new CancelShipmentCommand(Guid.NewGuid()), CancellationToken.None));

        VerifyNeverSaved(_unitOfWork);
    }

    [Theory]
    [InlineData(ShipmentStatus.Shipped)]
    [InlineData(ShipmentStatus.InTransit)]
    [InlineData(ShipmentStatus.Delivered)]
    [InlineData(ShipmentStatus.Failed)]
    [InlineData(ShipmentStatus.Returned)]
    [InlineData(ShipmentStatus.Cancelled)]
    public async Task Cancel_WhenShipmentIsNotPending_ThrowsConflict(ShipmentStatus status)
    {
        var handler = new CancelShipmentCommandHandler(_shipments.Object, _unitOfWork.Object);
        var shipment = ShippingFixtures.CreateShipment(status: status);
        SetupShipment(shipment);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new CancelShipmentCommand(shipment.Id), CancellationToken.None));

        Assert.Contains(
            $"Only pending shipments can be cancelled (current: {status}).", exception.Message);
        Assert.Equal(status, shipment.Status);
        VerifyNeverSaved(_unitOfWork);
    }
}
