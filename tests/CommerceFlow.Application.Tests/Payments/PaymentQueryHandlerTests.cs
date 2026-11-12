using CommerceFlow.Application.Payments.Interfaces;
using CommerceFlow.Application.Payments.Queries.GetPaymentById;
using CommerceFlow.Application.Payments.Queries.GetPayments;
using CommerceFlow.Application.Tests.Orders;
using CommerceFlow.Domain.Entities.Payments;
using CommerceFlow.Domain.Enums;
using Moq;

namespace CommerceFlow.Application.Tests.Payments;

public sealed class PaymentQueryHandlerTests
{
    private readonly Mock<IPaymentRepository> _payments = new();

    // ---------- GetPaymentById ----------

    [Fact]
    public async Task GetPaymentById_WhenPaymentDoesNotExist_ReturnsNull()
    {
        var handler = new GetPaymentByIdQueryHandler(_payments.Object);

        var result = await handler.Handle(
            new GetPaymentByIdQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetPaymentById_WhenPaymentExists_MapsAllFields()
    {
        var handler = new GetPaymentByIdQueryHandler(_payments.Object);
        var payment = PaymentFixtures.CreatePayment(status: PaymentStatus.Succeeded, amount: 123.45m);
        _payments
            .Setup(x => x.GetByIdAsync(payment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        var result = await handler.Handle(
            new GetPaymentByIdQuery(payment.Id), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(payment.Id, result.Id);
        Assert.Equal(PaymentFixtures.OrderId, result.OrderId);
        Assert.Equal(OrderFixtures.CustomerId, result.CustomerId);
        Assert.Equal(123.45m, result.Amount);
        Assert.Equal("USD", result.Currency);
        Assert.Equal(PaymentStatus.Succeeded, result.Status);
        Assert.Equal("Stripe", result.Provider);
        Assert.Equal("idem-key-123", result.IdempotencyKey);
        Assert.Equal("txn_123", result.ProviderTransactionId);
        Assert.Null(result.FailureReason);
        Assert.NotNull(result.ProcessedAtUtc);
        Assert.Null(result.RefundedAtUtc);
        Assert.Equal(payment.CreatedAtUtc, result.CreatedAtUtc);
        Assert.Equal(payment.UpdatedAtUtc, result.UpdatedAtUtc);
    }

    // ---------- GetPayments ----------

    [Fact]
    public async Task GetPayments_ReturnsPagedListMappedFromRepository()
    {
        var handler = new GetPaymentsQueryHandler(_payments.Object);
        var succeeded = PaymentFixtures.CreatePayment(status: PaymentStatus.Succeeded);
        var failed = PaymentFixtures.CreatePayment(
            status: PaymentStatus.Failed, idempotencyKey: "key-2");
        _payments
            .Setup(x => x.CountAsync(
                It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<PaymentStatus?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(25);
        _payments
            .Setup(x => x.GetAllAsync(
                It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<PaymentStatus?>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Payment> { succeeded, failed });

        var result = await handler.Handle(new GetPaymentsQuery(), CancellationToken.None);

        Assert.Equal(25, result.TotalCount);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(20, result.PageSize);
        Assert.Equal(2, result.TotalPages); // ceil(25 / 20)
        Assert.Equal(2, result.Items.Count);

        var first = result.Items[0];
        Assert.Equal(succeeded.Id, first.Id);
        Assert.Equal(PaymentStatus.Succeeded, first.Status);
        Assert.Equal("txn_123", first.ProviderTransactionId);

        Assert.Equal(failed.Id, result.Items[1].Id);
        Assert.Equal(PaymentStatus.Failed, result.Items[1].Status);
        Assert.Equal("Card declined", result.Items[1].FailureReason);
    }

    [Fact]
    public async Task GetPayments_WithFilters_PassesThemThroughToRepository()
    {
        var handler = new GetPaymentsQueryHandler(_payments.Object);
        var orderId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var payment = PaymentFixtures.CreatePayment(orderId, PaymentStatus.Failed);
        _payments
            .Setup(x => x.CountAsync(
                orderId, customerId, PaymentStatus.Failed, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _payments
            .Setup(x => x.GetAllAsync(
                orderId, customerId, PaymentStatus.Failed, 2, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Payment> { payment });

        var result = await handler.Handle(
            new GetPaymentsQuery(orderId, customerId, PaymentStatus.Failed, 2, 10),
            CancellationToken.None);

        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal(orderId, result.Items[0].OrderId);
        _payments.Verify(
            x => x.CountAsync(
                orderId, customerId, PaymentStatus.Failed, It.IsAny<CancellationToken>()),
            Times.Once);
        _payments.Verify(
            x => x.GetAllAsync(
                orderId, customerId, PaymentStatus.Failed, 2, 10, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
