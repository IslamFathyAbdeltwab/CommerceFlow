using CommerceFlow.Application.Payments.Commands.CreatePayment;
using CommerceFlow.Application.Payments.Commands.ProcessPayment;
using CommerceFlow.Application.Payments.Commands.RefundPayment;
using CommerceFlow.Application.Payments.Queries.GetPayments;
using CommerceFlow.Domain.Entities.Payments;

namespace CommerceFlow.Application.Tests.Payments;

public sealed class PaymentValidatorTests
{
    private static string TooLong(int length) => new('x', length);

    // ---------- CreatePayment ----------

    private static CreatePaymentCommand ValidCreate() =>
        PaymentFixtures.ValidCreateCommand();

    [Fact]
    public void Create_WithValidInput_IsValid()
    {
        var result = new CreatePaymentCommandValidator().Validate(ValidCreate());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Create_WithEmptyOrderId_IsInvalid()
    {
        var result = new CreatePaymentCommandValidator()
            .Validate(ValidCreate() with { OrderId = Guid.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreatePaymentCommand.OrderId));
    }

    [Fact]
    public void Create_WithZeroAmount_IsInvalid()
    {
        var result = new CreatePaymentCommandValidator()
            .Validate(ValidCreate() with { Amount = 0m });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreatePaymentCommand.Amount));
    }

    [Fact]
    public void Create_WithNegativeAmount_IsInvalid()
    {
        var result = new CreatePaymentCommandValidator()
            .Validate(ValidCreate() with { Amount = -10m });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreatePaymentCommand.Amount));
    }

    [Fact]
    public void Create_WithEmptyCurrency_IsInvalid()
    {
        var result = new CreatePaymentCommandValidator()
            .Validate(ValidCreate() with { Currency = "" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreatePaymentCommand.Currency));
    }

    [Fact]
    public void Create_WithTooLongCurrency_IsInvalid()
    {
        var result = new CreatePaymentCommandValidator()
            .Validate(ValidCreate() with { Currency = TooLong(Payment.CurrencyLength + 1) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreatePaymentCommand.Currency));
    }

    [Fact]
    public void Create_WithEmptyProvider_IsInvalid()
    {
        var result = new CreatePaymentCommandValidator()
            .Validate(ValidCreate() with { Provider = "   " });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreatePaymentCommand.Provider));
    }

    [Fact]
    public void Create_WithTooLongProvider_IsInvalid()
    {
        var result = new CreatePaymentCommandValidator()
            .Validate(ValidCreate() with { Provider = TooLong(Payment.ProviderMaxLength + 1) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreatePaymentCommand.Provider));
    }

    [Fact]
    public void Create_WithEmptyIdempotencyKey_IsInvalid()
    {
        var result = new CreatePaymentCommandValidator()
            .Validate(ValidCreate() with { IdempotencyKey = "" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreatePaymentCommand.IdempotencyKey));
    }

    [Fact]
    public void Create_WithTooLongIdempotencyKey_IsInvalid()
    {
        var result = new CreatePaymentCommandValidator()
            .Validate(ValidCreate() with
            {
                IdempotencyKey = TooLong(Payment.IdempotencyKeyMaxLength + 1)
            });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreatePaymentCommand.IdempotencyKey));
    }

    // ---------- ProcessPayment ----------

    [Fact]
    public void Process_WithValidInput_IsValid()
    {
        var result = new ProcessPaymentCommandValidator()
            .Validate(new ProcessPaymentCommand(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Process_WithEmptyPaymentId_IsInvalid()
    {
        var result = new ProcessPaymentCommandValidator()
            .Validate(new ProcessPaymentCommand(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ProcessPaymentCommand.PaymentId));
    }

    // ---------- RefundPayment ----------

    [Fact]
    public void Refund_WithValidInput_IsValid()
    {
        var result = new RefundPaymentCommandValidator()
            .Validate(new RefundPaymentCommand(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Refund_WithEmptyPaymentId_IsInvalid()
    {
        var result = new RefundPaymentCommandValidator()
            .Validate(new RefundPaymentCommand(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RefundPaymentCommand.PaymentId));
    }

    // ---------- GetPayments (query) ----------

    [Fact]
    public void GetPayments_WithDefaults_IsValid()
    {
        var result = new GetPaymentsQueryValidator().Validate(new GetPaymentsQuery());

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1001)]
    public void GetPayments_WithOutOfRangePageNumber_IsInvalid(int pageNumber)
    {
        var result = new GetPaymentsQueryValidator()
            .Validate(new GetPaymentsQuery(PageNumber: pageNumber));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetPaymentsQuery.PageNumber));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void GetPayments_WithOutOfRangePageSize_IsInvalid(int pageSize)
    {
        var result = new GetPaymentsQueryValidator()
            .Validate(new GetPaymentsQuery(PageSize: pageSize));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetPaymentsQuery.PageSize));
    }
}
