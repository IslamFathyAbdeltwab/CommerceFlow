using CommerceFlow.Application.Payments.Queries;
using MediatR;

namespace CommerceFlow.Application.Payments.Commands.RefundPayment;

// Refunds a succeeded payment through its gateway and syncs the order's
// payment status back to Refunded (fulfillment state is never touched -
// the two lifecycles stay independent). Returns the resulting state.
public sealed record RefundPaymentCommand(Guid PaymentId) : IRequest<PaymentDto>;
