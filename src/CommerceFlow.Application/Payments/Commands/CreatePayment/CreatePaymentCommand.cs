using MediatR;

namespace CommerceFlow.Application.Payments.Commands.CreatePayment;

// CustomerId is deliberately absent: the order is the single source of truth
// for who is paying (Payment.CustomerId is copied from it), so a client can
// never create a payment under the wrong customer.
// Returns the payment id; a replayed IdempotencyKey returns the ORIGINAL id.
public sealed record CreatePaymentCommand(
    Guid OrderId,
    decimal Amount,
    string Currency,
    string Provider,
    string IdempotencyKey) : IRequest<Guid>;
