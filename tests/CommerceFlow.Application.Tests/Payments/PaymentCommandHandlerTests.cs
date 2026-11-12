using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Orders.Interfaces;
using CommerceFlow.Application.Payments.Commands.CreatePayment;
using CommerceFlow.Application.Payments.Commands.ProcessPayment;
using CommerceFlow.Application.Payments.Commands.RefundPayment;
using CommerceFlow.Application.Payments.Gateways;
using CommerceFlow.Application.Payments.Interfaces;
using CommerceFlow.Application.Payments.Queries;
using CommerceFlow.Application.Tests.Orders;
using CommerceFlow.Domain.Entities.Orders;
using CommerceFlow.Domain.Entities.Payments;
using CommerceFlow.Domain.Enums;
using Moq;

namespace CommerceFlow.Application.Tests.Payments;

public sealed class PaymentCommandHandlerTests
{
    private readonly Mock<IPaymentRepository> _payments = new();
    private readonly Mock<IOrderRepository> _orders = new();
    private readonly Mock<IPaymentGatewayResolver> _gateways = new();
    private readonly Mock<IPaymentGateway> _gateway = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    public PaymentCommandHandlerTests()
    {
        // the supported provider; anything else resolves to null (unsupported)
        _gateway.Setup(x => x.Provider).Returns("Stripe");
        _gateways.Setup(x => x.Resolve("Stripe")).Returns(_gateway.Object);
    }

    private void SetupPayment(Payment payment) =>
        _payments
            .Setup(x => x.GetByIdAsync(payment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

    private void SetupOrder(Guid orderId, Order order) =>
        _orders
            .Setup(x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

    private void SetupNoReplay() =>
        _payments
            .Setup(x => x.GetByIdempotencyKeyAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment?)null);

    private void SetupGatewayCharge(
        bool succeeded,
        string transactionId = "txn_999",
        string failureReason = "Card declined") =>
        _gateway
            .Setup(x => x.ChargeAsync(
                It.IsAny<GatewayChargeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(succeeded
                ? GatewayChargeResult.Success(transactionId)
                : GatewayChargeResult.Failure(failureReason));

    private void SetupGatewayRefund(
        bool succeeded,
        string failureReason = "Refund window expired") =>
        _gateway
            .Setup(x => x.RefundAsync(
                It.IsAny<GatewayRefundRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(succeeded
                ? GatewayRefundResult.Success()
                : GatewayRefundResult.Failure(failureReason));

    // ---------- CreatePayment ----------

    [Fact]
    public async Task Create_WithValidInput_CreatesPendingPaymentAndSaves()
    {
        var handler = new CreatePaymentCommandHandler(
            _payments.Object, _orders.Object, _gateways.Object, _unitOfWork.Object);
        var order = OrderFixtures.CreateOrder(); // Unpaid, Pending, total 200 USD
        SetupOrder(order.Id, order);
        SetupNoReplay();

        var id = await handler.Handle(
            PaymentFixtures.ValidCreateCommand(
                order.Id, amount: 200m, currency: " usd ", idempotencyKey: "  key-1  "),
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, id);
        _payments.Verify(
            x => x.AddAsync(
                It.Is<Payment>(p => p.Id == id
                    && p.OrderId == order.Id
                    && p.CustomerId == order.CustomerId // copied from the order
                    && p.Amount == 200m
                    && p.Currency == "USD" // normalized from " usd "
                    && p.Status == PaymentStatus.Pending
                    && p.Provider == "Stripe"
                    && p.IdempotencyKey == "key-1"), // trimmed
                It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_WhenOrderConfirmed_IsStillAllowed()
    {
        var handler = new CreatePaymentCommandHandler(
            _payments.Object, _orders.Object, _gateways.Object, _unitOfWork.Object);
        var order = OrderFixtures.CreateOrder(OrderStatus.Confirmed);
        SetupOrder(order.Id, order);
        SetupNoReplay();

        var id = await handler.Handle(
            PaymentFixtures.ValidCreateCommand(order.Id),
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, id);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_WhenKeyReplayedWithSamePayload_ReturnsOriginalIdWithoutSaving()
    {
        var handler = new CreatePaymentCommandHandler(
            _payments.Object, _orders.Object, _gateways.Object, _unitOfWork.Object);
        var existing = PaymentFixtures.CreatePayment(amount: 200m, idempotencyKey: "key-1");
        _payments
            .Setup(x => x.GetByIdempotencyKeyAsync(
                existing.OrderId, "key-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var id = await handler.Handle(
            PaymentFixtures.ValidCreateCommand(amount: 200m, idempotencyKey: "key-1"),
            CancellationToken.None);

        Assert.Equal(existing.Id, id); // the original payment, not a new one
        _orders.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _payments.Verify(x => x.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_WhenKeyReplayedWithDifferentAmount_ThrowsConflict()
    {
        var handler = new CreatePaymentCommandHandler(
            _payments.Object, _orders.Object, _gateways.Object, _unitOfWork.Object);
        var existing = PaymentFixtures.CreatePayment(amount: 200m, idempotencyKey: "key-1");
        _payments
            .Setup(x => x.GetByIdempotencyKeyAsync(
                existing.OrderId, "key-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(
                PaymentFixtures.ValidCreateCommand(amount: 150m, idempotencyKey: "key-1"),
                CancellationToken.None));

        Assert.Contains(
            "Idempotency key was already used with a different request", exception.Message);
        _payments.Verify(x => x.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_WhenOrderDoesNotExist_ThrowsNotFound()
    {
        var handler = new CreatePaymentCommandHandler(
            _payments.Object, _orders.Object, _gateways.Object, _unitOfWork.Object);
        var orderId = Guid.NewGuid();
        SetupNoReplay();
        _orders
            .Setup(x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(PaymentFixtures.ValidCreateCommand(orderId), CancellationToken.None));

        _payments.Verify(x => x.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_WhenOrderAlreadyPaid_ThrowsConflict()
    {
        var handler = new CreatePaymentCommandHandler(
            _payments.Object, _orders.Object, _gateways.Object, _unitOfWork.Object);
        var order = OrderFixtures.CreatePaidOrder();
        SetupOrder(order.Id, order);
        SetupNoReplay();

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(PaymentFixtures.ValidCreateCommand(order.Id), CancellationToken.None));

        Assert.Contains("Order payment is already recorded", exception.Message);
        _payments.Verify(x => x.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_WhenOrderCancelled_ThrowsConflict()
    {
        var handler = new CreatePaymentCommandHandler(
            _payments.Object, _orders.Object, _gateways.Object, _unitOfWork.Object);
        var order = OrderFixtures.CreateOrder(OrderStatus.Cancelled);
        SetupOrder(order.Id, order);
        SetupNoReplay();

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(PaymentFixtures.ValidCreateCommand(order.Id), CancellationToken.None));

        Assert.Contains(
            "Only pending or confirmed orders can be marked as paid", exception.Message);
        _payments.Verify(x => x.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_WhenAmountDoesNotMatchOrderTotal_ThrowsConflict()
    {
        var handler = new CreatePaymentCommandHandler(
            _payments.Object, _orders.Object, _gateways.Object, _unitOfWork.Object);
        var order = OrderFixtures.CreateOrder(); // GrandTotal = 200
        SetupOrder(order.Id, order);
        SetupNoReplay();

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(
                PaymentFixtures.ValidCreateCommand(order.Id, amount: 150m),
                CancellationToken.None));

        Assert.Contains("Payment amount must match the order total", exception.Message);
        _payments.Verify(x => x.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_WhenCurrencyDoesNotMatchOrder_ThrowsConflict()
    {
        var handler = new CreatePaymentCommandHandler(
            _payments.Object, _orders.Object, _gateways.Object, _unitOfWork.Object);
        var order = OrderFixtures.CreateOrder(); // USD
        SetupOrder(order.Id, order);
        SetupNoReplay();

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(
                PaymentFixtures.ValidCreateCommand(order.Id, currency: "EUR"),
                CancellationToken.None));

        Assert.Contains("Payment currency must match the order currency", exception.Message);
        _payments.Verify(x => x.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_WhenProviderNotSupported_ThrowsConflict()
    {
        var handler = new CreatePaymentCommandHandler(
            _payments.Object, _orders.Object, _gateways.Object, _unitOfWork.Object);
        var order = OrderFixtures.CreateOrder();
        SetupOrder(order.Id, order);
        SetupNoReplay();
        // resolver only knows "Stripe" -> "UnknownPay" resolves to null

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(
                PaymentFixtures.ValidCreateCommand(order.Id, provider: "UnknownPay"),
                CancellationToken.None));

        Assert.Contains("Payment provider is not supported", exception.Message);
        _payments.Verify(x => x.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---------- ProcessPayment ----------

    [Fact]
    public async Task Process_WhenGatewaySucceeds_MarksPaymentAndOrderPaidAndSaves()
    {
        var handler = new ProcessPaymentCommandHandler(
            _payments.Object, _orders.Object, _gateways.Object, _unitOfWork.Object);
        var order = OrderFixtures.CreateOrder(); // Unpaid, Pending
        var payment = PaymentFixtures.CreatePayment(order.Id);
        SetupPayment(payment);
        SetupOrder(payment.OrderId, order);
        SetupGatewayCharge(succeeded: true, transactionId: "txn_999");

        var dto = await handler.Handle(
            new ProcessPaymentCommand(payment.Id), CancellationToken.None);

        // payment side
        Assert.Equal(PaymentStatus.Succeeded, payment.Status);
        Assert.Equal("txn_999", payment.ProviderTransactionId);
        Assert.NotNull(payment.ProcessedAtUtc);
        // order side: coarse view synced, fulfillment untouched
        Assert.Equal(OrderPaymentStatus.Paid, order.PaymentStatus);
        Assert.NotNull(order.PaidAtUtc);
        Assert.Equal(OrderStatus.Pending, order.Status);
        // the gateway received the full payment context
        _gateway.Verify(
            x => x.ChargeAsync(
                It.Is<GatewayChargeRequest>(r =>
                    r.PaymentId == payment.Id
                    && r.CustomerId == payment.CustomerId
                    && r.Amount == payment.Amount
                    && r.Currency == payment.Currency
                    && r.IdempotencyKey == payment.IdempotencyKey),
                It.IsAny<CancellationToken>()),
            Times.Once);
        // the response carries the outcome
        Assert.Equal(PaymentStatus.Succeeded, dto.Status);
        Assert.Equal("txn_999", dto.ProviderTransactionId);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Process_WhenGatewayDeclines_MarksFailedAndLeavesOrderUnpaid()
    {
        var handler = new ProcessPaymentCommandHandler(
            _payments.Object, _orders.Object, _gateways.Object, _unitOfWork.Object);
        var order = OrderFixtures.CreateOrder();
        var payment = PaymentFixtures.CreatePayment(order.Id);
        SetupPayment(payment);
        SetupOrder(payment.OrderId, order);
        SetupGatewayCharge(succeeded: false, failureReason: "Insufficient funds");

        var dto = await handler.Handle(
            new ProcessPaymentCommand(payment.Id), CancellationToken.None);

        Assert.Equal(PaymentStatus.Failed, payment.Status);
        Assert.Equal("Insufficient funds", payment.FailureReason);
        Assert.NotNull(payment.ProcessedAtUtc);
        // a decline never touches the order
        Assert.Equal(OrderPaymentStatus.Unpaid, order.PaymentStatus);
        Assert.Null(order.PaidAtUtc);
        Assert.Equal(PaymentStatus.Failed, dto.Status);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Process_WhenPaymentDoesNotExist_ThrowsNotFound()
    {
        var handler = new ProcessPaymentCommandHandler(
            _payments.Object, _orders.Object, _gateways.Object, _unitOfWork.Object);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new ProcessPaymentCommand(Guid.NewGuid()), CancellationToken.None));

        _gateway.Verify(
            x => x.ChargeAsync(It.IsAny<GatewayChargeRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Process_WhenPaymentAlreadyProcessed_ThrowsConflictBeforeGateway()
    {
        var handler = new ProcessPaymentCommandHandler(
            _payments.Object, _orders.Object, _gateways.Object, _unitOfWork.Object);
        var payment = PaymentFixtures.CreatePayment(status: PaymentStatus.Succeeded);
        SetupPayment(payment);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new ProcessPaymentCommand(payment.Id), CancellationToken.None));

        Assert.Contains("Payment has already been processed", exception.Message);
        // money safety: nothing is loaded or charged after the state check
        _orders.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _gateway.Verify(
            x => x.ChargeAsync(It.IsAny<GatewayChargeRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Process_WhenOrderDoesNotExist_ThrowsNotFoundBeforeGateway()
    {
        var handler = new ProcessPaymentCommandHandler(
            _payments.Object, _orders.Object, _gateways.Object, _unitOfWork.Object);
        var payment = PaymentFixtures.CreatePayment();
        SetupPayment(payment);
        _orders
            .Setup(x => x.GetByIdAsync(payment.OrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new ProcessPaymentCommand(payment.Id), CancellationToken.None));

        _gateway.Verify(
            x => x.ChargeAsync(It.IsAny<GatewayChargeRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Process_WhenOrderAlreadyPaid_ThrowsConflictBeforeGateway()
    {
        var handler = new ProcessPaymentCommandHandler(
            _payments.Object, _orders.Object, _gateways.Object, _unitOfWork.Object);
        var order = OrderFixtures.CreatePaidOrder();
        var payment = PaymentFixtures.CreatePayment(order.Id);
        SetupPayment(payment);
        SetupOrder(payment.OrderId, order);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new ProcessPaymentCommand(payment.Id), CancellationToken.None));

        Assert.Contains("Order payment is already recorded", exception.Message);
        _gateway.Verify(
            x => x.ChargeAsync(It.IsAny<GatewayChargeRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Process_WhenOrderCancelled_ThrowsConflictBeforeGateway()
    {
        var handler = new ProcessPaymentCommandHandler(
            _payments.Object, _orders.Object, _gateways.Object, _unitOfWork.Object);
        var order = OrderFixtures.CreateOrder(OrderStatus.Cancelled);
        var payment = PaymentFixtures.CreatePayment(order.Id);
        SetupPayment(payment);
        SetupOrder(payment.OrderId, order);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new ProcessPaymentCommand(payment.Id), CancellationToken.None));

        Assert.Contains(
            "Only pending or confirmed orders can be marked as paid", exception.Message);
        _gateway.Verify(
            x => x.ChargeAsync(It.IsAny<GatewayChargeRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Process_WhenProviderUnknown_ThrowsConflictBeforeGateway()
    {
        var handler = new ProcessPaymentCommandHandler(
            _payments.Object, _orders.Object, _gateways.Object, _unitOfWork.Object);
        var order = OrderFixtures.CreateOrder();
        var payment = PaymentFixtures.CreatePayment(order.Id, provider: "UnknownPay");
        SetupPayment(payment);
        SetupOrder(payment.OrderId, order);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new ProcessPaymentCommand(payment.Id), CancellationToken.None));

        Assert.Contains("Payment provider is not supported", exception.Message);
        _gateway.Verify(
            x => x.ChargeAsync(It.IsAny<GatewayChargeRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---------- RefundPayment ----------

    [Fact]
    public async Task Refund_WhenGatewayAccepts_RefundsPaymentAndOrderAndSaves()
    {
        var handler = new RefundPaymentCommandHandler(
            _payments.Object, _orders.Object, _gateways.Object, _unitOfWork.Object);
        var order = OrderFixtures.CreatePaidOrder(); // Paid, fulfillment Pending
        var payment = PaymentFixtures.CreatePayment(order.Id, PaymentStatus.Succeeded);
        SetupPayment(payment);
        SetupOrder(payment.OrderId, order);
        SetupGatewayRefund(succeeded: true);

        var dto = await handler.Handle(
            new RefundPaymentCommand(payment.Id), CancellationToken.None);

        // payment side
        Assert.Equal(PaymentStatus.Refunded, payment.Status);
        Assert.NotNull(payment.RefundedAtUtc);
        Assert.Equal("txn_123", payment.ProviderTransactionId); // audit trail kept
        // order side: payment view refunded, fulfillment untouched
        Assert.Equal(OrderPaymentStatus.Refunded, order.PaymentStatus);
        Assert.NotNull(order.RefundedAtUtc);
        Assert.Equal(OrderStatus.Pending, order.Status);
        // gateway received the original transaction reference
        _gateway.Verify(
            x => x.RefundAsync(
                It.Is<GatewayRefundRequest>(r =>
                    r.PaymentId == payment.Id
                    && r.ProviderTransactionId == "txn_123"
                    && r.Amount == payment.Amount
                    && r.Currency == payment.Currency),
                It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.Equal(PaymentStatus.Refunded, dto.Status);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Refund_WhenGatewayRejects_ThrowsConflictAndNothingChanges()
    {
        var handler = new RefundPaymentCommandHandler(
            _payments.Object, _orders.Object, _gateways.Object, _unitOfWork.Object);
        var order = OrderFixtures.CreatePaidOrder();
        var payment = PaymentFixtures.CreatePayment(order.Id, PaymentStatus.Succeeded);
        SetupPayment(payment);
        SetupOrder(payment.OrderId, order);
        SetupGatewayRefund(succeeded: false, failureReason: "Refund window expired");

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new RefundPaymentCommand(payment.Id), CancellationToken.None));

        Assert.Contains("Refund window expired", exception.Message); // provider's reason
        Assert.Equal(PaymentStatus.Succeeded, payment.Status); // untouched
        Assert.Null(payment.RefundedAtUtc);
        Assert.Equal(OrderPaymentStatus.Paid, order.PaymentStatus); // untouched
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Refund_WhenPaymentDoesNotExist_ThrowsNotFound()
    {
        var handler = new RefundPaymentCommandHandler(
            _payments.Object, _orders.Object, _gateways.Object, _unitOfWork.Object);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new RefundPaymentCommand(Guid.NewGuid()), CancellationToken.None));

        _gateway.Verify(
            x => x.RefundAsync(It.IsAny<GatewayRefundRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Refund_WhenPaymentAlreadyRefunded_ThrowsConflictBeforeGateway()
    {
        var handler = new RefundPaymentCommandHandler(
            _payments.Object, _orders.Object, _gateways.Object, _unitOfWork.Object);
        var payment = PaymentFixtures.CreatePayment(status: PaymentStatus.Refunded);
        SetupPayment(payment);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new RefundPaymentCommand(payment.Id), CancellationToken.None));

        Assert.Contains("Payment has already been refunded", exception.Message);
        _gateway.Verify(
            x => x.RefundAsync(It.IsAny<GatewayRefundRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Refund_WhenPaymentNotSucceeded_ThrowsConflictBeforeGateway()
    {
        var handler = new RefundPaymentCommandHandler(
            _payments.Object, _orders.Object, _gateways.Object, _unitOfWork.Object);
        var payment = PaymentFixtures.CreatePayment(status: PaymentStatus.Pending);
        SetupPayment(payment);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new RefundPaymentCommand(payment.Id), CancellationToken.None));

        Assert.Contains("Only succeeded payments can be refunded", exception.Message);
        _gateway.Verify(
            x => x.RefundAsync(It.IsAny<GatewayRefundRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Refund_WhenOrderDoesNotExist_ThrowsNotFoundBeforeGateway()
    {
        var handler = new RefundPaymentCommandHandler(
            _payments.Object, _orders.Object, _gateways.Object, _unitOfWork.Object);
        var payment = PaymentFixtures.CreatePayment(status: PaymentStatus.Succeeded);
        SetupPayment(payment);
        _orders
            .Setup(x => x.GetByIdAsync(payment.OrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new RefundPaymentCommand(payment.Id), CancellationToken.None));

        _gateway.Verify(
            x => x.RefundAsync(It.IsAny<GatewayRefundRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Refund_WhenOrderNotPaid_ThrowsConflictBeforeGateway()
    {
        var handler = new RefundPaymentCommandHandler(
            _payments.Object, _orders.Object, _gateways.Object, _unitOfWork.Object);
        var order = OrderFixtures.CreateOrder(); // Unpaid (desynced state)
        var payment = PaymentFixtures.CreatePayment(order.Id, PaymentStatus.Succeeded);
        SetupPayment(payment);
        SetupOrder(payment.OrderId, order);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new RefundPaymentCommand(payment.Id), CancellationToken.None));

        Assert.Contains("Order payment is not recorded as paid", exception.Message);
        _gateway.Verify(
            x => x.RefundAsync(It.IsAny<GatewayRefundRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Refund_WhenProviderUnknown_ThrowsConflictBeforeGateway()
    {
        var handler = new RefundPaymentCommandHandler(
            _payments.Object, _orders.Object, _gateways.Object, _unitOfWork.Object);
        var order = OrderFixtures.CreatePaidOrder();
        var payment = PaymentFixtures.CreatePayment(
            order.Id, PaymentStatus.Succeeded, provider: "UnknownPay");
        SetupPayment(payment);
        SetupOrder(payment.OrderId, order);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new RefundPaymentCommand(payment.Id), CancellationToken.None));

        Assert.Contains("Payment provider is not supported", exception.Message);
        _gateway.Verify(
            x => x.RefundAsync(It.IsAny<GatewayRefundRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
