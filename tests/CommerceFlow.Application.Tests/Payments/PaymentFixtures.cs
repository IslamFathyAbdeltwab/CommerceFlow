using CommerceFlow.Application.Payments.Commands.CreatePayment;
using CommerceFlow.Application.Tests.Orders;
using CommerceFlow.Domain.Entities.Payments;
using CommerceFlow.Domain.Enums;

namespace CommerceFlow.Application.Tests.Payments;

// Shared factories for building payments (and the create command) in tests.
// Default amount matches OrderFixtures' GrandTotal (100 x 2 = 200 USD) so a
// payment lines up with its order out of the box.
internal static class PaymentFixtures
{
    public static readonly Guid OrderId = Guid.Parse("77777777-7777-7777-7777-777777777777");

    public const decimal OrderTotal = 200m;

    public static Payment CreatePayment(
        Guid? orderId = null,
        PaymentStatus status = PaymentStatus.Pending,
        decimal amount = OrderTotal,
        string currency = "USD",
        string provider = "Stripe",
        string idempotencyKey = "idem-key-123")
    {
        var payment = Payment.Create(
            orderId ?? OrderId,
            OrderFixtures.CustomerId,
            amount,
            currency,
            provider,
            idempotencyKey);

        switch (status)
        {
            case PaymentStatus.Pending:
                break;
            case PaymentStatus.Succeeded:
                payment.MarkSucceeded("txn_123");
                break;
            case PaymentStatus.Failed:
                payment.MarkFailed("Card declined");
                break;
            case PaymentStatus.Refunded:
                payment.MarkSucceeded("txn_123");
                payment.Refund();
                break;
        }

        return payment;
    }

    public static CreatePaymentCommand ValidCreateCommand(
        Guid? orderId = null,
        decimal amount = OrderTotal,
        string currency = "USD",
        string provider = "Stripe",
        string idempotencyKey = "idem-key-123") =>
        new(orderId ?? OrderId, amount, currency, provider, idempotencyKey);
}
