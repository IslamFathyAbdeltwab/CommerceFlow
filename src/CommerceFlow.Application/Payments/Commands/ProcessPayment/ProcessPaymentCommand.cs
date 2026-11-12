using CommerceFlow.Application.Payments.Queries;
using MediatR;

namespace CommerceFlow.Application.Payments.Commands.ProcessPayment;

// Charges the payment through its provider gateway (Strategy) and syncs the
// order. Returns the resulting payment state so the client learns the
// gateway outcome in the same call instead of polling with a GET.
public sealed record ProcessPaymentCommand(Guid PaymentId) : IRequest<PaymentDto>;
