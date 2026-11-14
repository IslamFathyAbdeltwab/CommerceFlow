using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Orders.Interfaces;
using CommerceFlow.Application.Returns.Commands.ApproveReturn;
using CommerceFlow.Application.Returns.Commands.CancelReturn;
using CommerceFlow.Application.Returns.Commands.CompleteReturn;
using CommerceFlow.Application.Returns.Commands.CreateReturnRequest;
using CommerceFlow.Application.Returns.Commands.ReceiveReturn;
using CommerceFlow.Application.Returns.Commands.RejectReturn;
using CommerceFlow.Application.Returns.Interfaces;
using CommerceFlow.Application.Tests.Orders;
using CommerceFlow.Domain.Entities.Orders;
using CommerceFlow.Domain.Entities.Returns;
using CommerceFlow.Domain.Enums;
using CommerceFlow.Domain.Exceptions;
using Moq;

namespace CommerceFlow.Application.Tests.Returns;

public sealed class ReturnsCommandHandlerTests
{
    private readonly Mock<IReturnRequestRepository> _returns = new();
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

    private void SetupReturn(ReturnRequest request) =>
        _returns
            .Setup(x => x.GetByIdAsync(request.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);

    private void SetupReturnMissing(Guid id) =>
        _returns
            .Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ReturnRequest?)null);

    // every create test that reaches the quantity pre-check needs this:
    // loose Moq would hand the handler a null list
    private void SetupExistingReturns(Guid orderId, params ReturnRequest[] existing) =>
        _returns
            .Setup(x => x.GetByOrderIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing.ToList());

    private static void VerifyNeverSaved(Mock<IUnitOfWork> unitOfWork) =>
        unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

    // ---------- CreateReturnRequest ----------

    [Fact]
    public async Task Create_WithValidInput_CreatesPendingRequestAndSaves()
    {
        var handler = new CreateReturnRequestCommandHandler(
            _returns.Object, _orders.Object, _unitOfWork.Object);
        var order = OrderFixtures.CreateOrder(OrderStatus.Delivered); // line qty 2
        var variantId = order.Lines.First().ProductVariantId;
        SetupOrder(order.Id, order);
        SetupExistingReturns(order.Id);

        var id = await handler.Handle(
            ReturnsFixtures.ValidCreateCommand(
                order.Id,
                variantId,
                quantity: 2,
                notes: "  Box crushed in transit.  "),
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, id);
        _returns.Verify(
            x => x.AddAsync(
                It.Is<ReturnRequest>(r => r.Id == id
                    && r.OrderId == order.Id
                    && r.CustomerId == order.CustomerId // copied from the order
                    && r.Status == ReturnStatus.Pending
                    && r.Reason == ReturnReason.Damaged
                    && r.Notes == "Box crushed in transit." // trimmed
                    && r.Items.Count == 1
                    && r.Items.Single().ProductVariantId == variantId
                    && r.Items.Single().Quantity == 2),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_WhenOrderDoesNotExist_ThrowsNotFound()
    {
        var handler = new CreateReturnRequestCommandHandler(
            _returns.Object, _orders.Object, _unitOfWork.Object);
        var orderId = Guid.NewGuid();
        SetupOrderMissing(orderId);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                ReturnsFixtures.ValidCreateCommand(orderId),
                CancellationToken.None));

        _returns.Verify(
            x => x.GetByOrderIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _returns.Verify(
            x => x.AddAsync(It.IsAny<ReturnRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
        VerifyNeverSaved(_unitOfWork);
    }

    [Theory]
    [InlineData(OrderStatus.Pending)]
    [InlineData(OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Shipped)]
    [InlineData(OrderStatus.Cancelled)]
    public async Task Create_WhenOrderIsNotDelivered_ThrowsConflict(OrderStatus status)
    {
        var handler = new CreateReturnRequestCommandHandler(
            _returns.Object, _orders.Object, _unitOfWork.Object);
        var order = OrderFixtures.CreateOrder(status);
        SetupOrder(order.Id, order);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(
                ReturnsFixtures.ValidCreateCommand(order.Id),
                CancellationToken.None));

        Assert.Contains(
            $"Only delivered orders can be returned (current: {status}).",
            exception.Message);
        _returns.Verify(
            x => x.GetByOrderIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _returns.Verify(
            x => x.AddAsync(It.IsAny<ReturnRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
        VerifyNeverSaved(_unitOfWork);
    }

    [Fact]
    public async Task Create_WhenItemDoesNotMatchAnyOrderLine_ThrowsConflict()
    {
        var handler = new CreateReturnRequestCommandHandler(
            _returns.Object, _orders.Object, _unitOfWork.Object);
        var order = OrderFixtures.CreateOrder(OrderStatus.Delivered);
        SetupOrder(order.Id, order);
        SetupExistingReturns(order.Id);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(
                ReturnsFixtures.ValidCreateCommand(order.Id, productVariantId: Guid.NewGuid()),
                CancellationToken.None));

        Assert.Contains("Return item does not match any order line.", exception.Message);
        _returns.Verify(
            x => x.AddAsync(It.IsAny<ReturnRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
        VerifyNeverSaved(_unitOfWork);
    }

    [Fact]
    public async Task Create_WhenQuantityExceedsWhatWasOrdered_ThrowsConflict()
    {
        var handler = new CreateReturnRequestCommandHandler(
            _returns.Object, _orders.Object, _unitOfWork.Object);
        var order = OrderFixtures.CreateOrder(OrderStatus.Delivered); // line qty 2
        var variantId = order.Lines.First().ProductVariantId;
        SetupOrder(order.Id, order);
        SetupExistingReturns(order.Id);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(
                ReturnsFixtures.ValidCreateCommand(order.Id, variantId, quantity: 3),
                CancellationToken.None));

        Assert.Contains("Return quantity exceeds the returnable quantity.", exception.Message);
        _returns.Verify(
            x => x.AddAsync(It.IsAny<ReturnRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
        VerifyNeverSaved(_unitOfWork);
    }

    [Fact]
    public async Task Create_WhenActiveRequestStillHoldsTheQuantity_ThrowsConflict()
    {
        var handler = new CreateReturnRequestCommandHandler(
            _returns.Object, _orders.Object, _unitOfWork.Object);
        var order = OrderFixtures.CreateOrder(OrderStatus.Delivered); // line qty 2
        var variantId = order.Lines.First().ProductVariantId;
        SetupOrder(order.Id, order);
        // an approved request already claims the whole line
        SetupExistingReturns(
            order.Id,
            ReturnsFixtures.CreateReturnRequest(
                orderId: order.Id,
                status: ReturnStatus.Approved,
                productVariantId: variantId,
                quantity: 2));

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(
                ReturnsFixtures.ValidCreateCommand(order.Id, variantId, quantity: 1),
                CancellationToken.None));

        Assert.Contains("Return quantity exceeds the returnable quantity.", exception.Message);
        _returns.Verify(
            x => x.AddAsync(It.IsAny<ReturnRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
        VerifyNeverSaved(_unitOfWork);
    }

    [Fact]
    public async Task Create_WhenExistingRequestWasRejectedOrCancelled_ReleasesQuantityClaim()
    {
        var handler = new CreateReturnRequestCommandHandler(
            _returns.Object, _orders.Object, _unitOfWork.Object);
        var order = OrderFixtures.CreateOrder(OrderStatus.Delivered); // line qty 2
        var variantId = order.Lines.First().ProductVariantId;
        SetupOrder(order.Id, order);
        SetupExistingReturns(
            order.Id,
            ReturnsFixtures.CreateReturnRequest(
                orderId: order.Id,
                status: ReturnStatus.Rejected,
                productVariantId: variantId,
                quantity: 2),
            ReturnsFixtures.CreateReturnRequest(
                orderId: order.Id,
                status: ReturnStatus.Cancelled,
                productVariantId: variantId,
                quantity: 2));

        var id = await handler.Handle(
            ReturnsFixtures.ValidCreateCommand(order.Id, variantId, quantity: 2),
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, id);
        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_WhenCompletedRequestTookTheGoods_ClaimStaysClaimed()
    {
        var handler = new CreateReturnRequestCommandHandler(
            _returns.Object, _orders.Object, _unitOfWork.Object);
        var order = OrderFixtures.CreateOrder(OrderStatus.Delivered); // line qty 2
        var variantId = order.Lines.First().ProductVariantId;
        SetupOrder(order.Id, order);
        // the goods are already back with us - they cannot go back again
        SetupExistingReturns(
            order.Id,
            ReturnsFixtures.CreateReturnRequest(
                orderId: order.Id,
                status: ReturnStatus.Completed,
                productVariantId: variantId,
                quantity: 2));

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(
                ReturnsFixtures.ValidCreateCommand(order.Id, variantId, quantity: 1),
                CancellationToken.None));

        Assert.Contains("Return quantity exceeds the returnable quantity.", exception.Message);
        VerifyNeverSaved(_unitOfWork);
    }

    // ---------- ApproveReturn ----------

    [Fact]
    public async Task Approve_FromPending_ApprovesAndSaves()
    {
        var handler = new ApproveReturnCommandHandler(_returns.Object, _unitOfWork.Object);
        var request = ReturnsFixtures.CreateReturnRequest();
        SetupReturn(request);

        await handler.Handle(new ApproveReturnCommand(request.Id), CancellationToken.None);

        Assert.Equal(ReturnStatus.Approved, request.Status);
        Assert.NotNull(request.ApprovedAtUtc);
        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Approve_WhenReturnDoesNotExist_ThrowsNotFound()
    {
        var handler = new ApproveReturnCommandHandler(_returns.Object, _unitOfWork.Object);
        SetupReturnMissing(Guid.NewGuid());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new ApproveReturnCommand(Guid.NewGuid()), CancellationToken.None));

        VerifyNeverSaved(_unitOfWork);
    }

    [Theory]
    [InlineData(ReturnStatus.Approved)]
    [InlineData(ReturnStatus.Rejected)]
    [InlineData(ReturnStatus.Received)]
    [InlineData(ReturnStatus.Completed)]
    [InlineData(ReturnStatus.Cancelled)]
    public async Task Approve_WhenReturnIsNotPending_ThrowsConflict(ReturnStatus status)
    {
        var handler = new ApproveReturnCommandHandler(_returns.Object, _unitOfWork.Object);
        var request = ReturnsFixtures.CreateReturnRequest(status: status);
        SetupReturn(request);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new ApproveReturnCommand(request.Id), CancellationToken.None));

        Assert.Contains(
            $"Only pending return requests can be approved (current: {status}).",
            exception.Message);
        Assert.Equal(status, request.Status); // untouched
        VerifyNeverSaved(_unitOfWork);
    }

    // ---------- RejectReturn ----------

    [Fact]
    public async Task Reject_FromPending_RejectsWithTrimmedReasonAndSaves()
    {
        var handler = new RejectReturnCommandHandler(_returns.Object, _unitOfWork.Object);
        var request = ReturnsFixtures.CreateReturnRequest();
        SetupReturn(request);

        await handler.Handle(
            new RejectReturnCommand(request.Id, "  Outside the return window.  "),
            CancellationToken.None);

        Assert.Equal(ReturnStatus.Rejected, request.Status);
        Assert.Equal("Outside the return window.", request.RejectionReason); // trimmed
        Assert.NotNull(request.RejectedAtUtc);
        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Reject_WhenReturnDoesNotExist_ThrowsNotFound()
    {
        var handler = new RejectReturnCommandHandler(_returns.Object, _unitOfWork.Object);
        SetupReturnMissing(Guid.NewGuid());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new RejectReturnCommand(Guid.NewGuid(), "Outside the window."),
                CancellationToken.None));

        VerifyNeverSaved(_unitOfWork);
    }

    [Theory]
    [InlineData(ReturnStatus.Approved)]
    [InlineData(ReturnStatus.Rejected)]
    [InlineData(ReturnStatus.Received)]
    [InlineData(ReturnStatus.Completed)]
    [InlineData(ReturnStatus.Cancelled)]
    public async Task Reject_WhenReturnIsNotPending_ThrowsConflict(ReturnStatus status)
    {
        var handler = new RejectReturnCommandHandler(_returns.Object, _unitOfWork.Object);
        var request = ReturnsFixtures.CreateReturnRequest(status: status);
        SetupReturn(request);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(
                new RejectReturnCommand(request.Id, "Late notice."),
                CancellationToken.None));

        Assert.Contains(
            $"Only pending return requests can be rejected (current: {status}).",
            exception.Message);
        Assert.Equal(status, request.Status); // untouched
        VerifyNeverSaved(_unitOfWork);
    }

    // ---------- ReceiveReturn ----------

    [Fact]
    public async Task Receive_FromApproved_MarksReceivedAndSaves()
    {
        var handler = new ReceiveReturnCommandHandler(_returns.Object, _unitOfWork.Object);
        var request = ReturnsFixtures.CreateReturnRequest(status: ReturnStatus.Approved);
        SetupReturn(request);

        await handler.Handle(new ReceiveReturnCommand(request.Id), CancellationToken.None);

        Assert.Equal(ReturnStatus.Received, request.Status);
        Assert.NotNull(request.ReceivedAtUtc);
        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Receive_WhenReturnDoesNotExist_ThrowsNotFound()
    {
        var handler = new ReceiveReturnCommandHandler(_returns.Object, _unitOfWork.Object);
        SetupReturnMissing(Guid.NewGuid());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new ReceiveReturnCommand(Guid.NewGuid()), CancellationToken.None));

        VerifyNeverSaved(_unitOfWork);
    }

    [Theory]
    [InlineData(ReturnStatus.Pending)]
    [InlineData(ReturnStatus.Rejected)]
    [InlineData(ReturnStatus.Received)]
    [InlineData(ReturnStatus.Completed)]
    [InlineData(ReturnStatus.Cancelled)]
    public async Task Receive_WhenReturnIsNotApproved_ThrowsConflict(ReturnStatus status)
    {
        var handler = new ReceiveReturnCommandHandler(_returns.Object, _unitOfWork.Object);
        var request = ReturnsFixtures.CreateReturnRequest(status: status);
        SetupReturn(request);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new ReceiveReturnCommand(request.Id), CancellationToken.None));

        Assert.Contains(
            $"Only approved return requests can be received (current: {status}).",
            exception.Message);
        Assert.Equal(status, request.Status); // untouched
        VerifyNeverSaved(_unitOfWork);
    }

    [Fact]
    public async Task Receive_WhenBasketIsEmpty_ThrowsDomainExceptionWithoutSaving()
    {
        var handler = new ReceiveReturnCommandHandler(_returns.Object, _unitOfWork.Object);
        var request = ReturnsFixtures.CreateReturnRequest(
            status: ReturnStatus.Approved, withItem: false);
        SetupReturn(request);

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new ReceiveReturnCommand(request.Id), CancellationToken.None));

        Assert.Equal(
            "Return request must contain at least one item before it can be received.",
            exception.Message);
        Assert.Equal(ReturnStatus.Approved, request.Status); // state did not move
        VerifyNeverSaved(_unitOfWork);
    }

    // ---------- CompleteReturn ----------

    [Fact]
    public async Task Complete_FromReceived_CompletesAndSaves()
    {
        var handler = new CompleteReturnCommandHandler(_returns.Object, _unitOfWork.Object);
        var request = ReturnsFixtures.CreateReturnRequest(status: ReturnStatus.Received);
        SetupReturn(request);

        await handler.Handle(new CompleteReturnCommand(request.Id), CancellationToken.None);

        Assert.Equal(ReturnStatus.Completed, request.Status);
        Assert.NotNull(request.CompletedAtUtc);
        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Complete_WhenReturnDoesNotExist_ThrowsNotFound()
    {
        var handler = new CompleteReturnCommandHandler(_returns.Object, _unitOfWork.Object);
        SetupReturnMissing(Guid.NewGuid());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new CompleteReturnCommand(Guid.NewGuid()), CancellationToken.None));

        VerifyNeverSaved(_unitOfWork);
    }

    [Theory]
    [InlineData(ReturnStatus.Pending)]
    [InlineData(ReturnStatus.Approved)]
    [InlineData(ReturnStatus.Rejected)]
    [InlineData(ReturnStatus.Completed)]
    [InlineData(ReturnStatus.Cancelled)]
    public async Task Complete_WhenReturnIsNotReceived_ThrowsConflict(ReturnStatus status)
    {
        var handler = new CompleteReturnCommandHandler(_returns.Object, _unitOfWork.Object);
        var request = ReturnsFixtures.CreateReturnRequest(status: status);
        SetupReturn(request);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new CompleteReturnCommand(request.Id), CancellationToken.None));

        Assert.Contains(
            $"Only received return requests can be completed (current: {status}).",
            exception.Message);
        Assert.Equal(status, request.Status); // untouched
        VerifyNeverSaved(_unitOfWork);
    }

    // ---------- CancelReturn ----------

    [Fact]
    public async Task Cancel_FromPending_CancelsAndSaves()
    {
        var handler = new CancelReturnCommandHandler(_returns.Object, _unitOfWork.Object);
        var request = ReturnsFixtures.CreateReturnRequest();
        SetupReturn(request);

        await handler.Handle(new CancelReturnCommand(request.Id), CancellationToken.None);

        Assert.Equal(ReturnStatus.Cancelled, request.Status);
        Assert.NotNull(request.CancelledAtUtc);
        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Cancel_FromApproved_RevokesAndSaves()
    {
        var handler = new CancelReturnCommandHandler(_returns.Object, _unitOfWork.Object);
        var request = ReturnsFixtures.CreateReturnRequest(status: ReturnStatus.Approved);
        SetupReturn(request);

        await handler.Handle(new CancelReturnCommand(request.Id), CancellationToken.None);

        Assert.Equal(ReturnStatus.Cancelled, request.Status);
        Assert.NotNull(request.ApprovedAtUtc); // audit trail kept
        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Cancel_WhenReturnDoesNotExist_ThrowsNotFound()
    {
        var handler = new CancelReturnCommandHandler(_returns.Object, _unitOfWork.Object);
        SetupReturnMissing(Guid.NewGuid());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new CancelReturnCommand(Guid.NewGuid()), CancellationToken.None));

        VerifyNeverSaved(_unitOfWork);
    }

    [Theory]
    [InlineData(ReturnStatus.Received)]
    [InlineData(ReturnStatus.Rejected)]
    [InlineData(ReturnStatus.Completed)]
    [InlineData(ReturnStatus.Cancelled)]
    public async Task Cancel_WhenReturnIsNotPendingOrApproved_ThrowsConflict(ReturnStatus status)
    {
        var handler = new CancelReturnCommandHandler(_returns.Object, _unitOfWork.Object);
        var request = ReturnsFixtures.CreateReturnRequest(status: status);
        SetupReturn(request);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new CancelReturnCommand(request.Id), CancellationToken.None));

        Assert.Contains(
            $"Only pending or approved return requests can be cancelled (current: {status}).",
            exception.Message);
        Assert.Equal(status, request.Status); // untouched
        VerifyNeverSaved(_unitOfWork);
    }
}
