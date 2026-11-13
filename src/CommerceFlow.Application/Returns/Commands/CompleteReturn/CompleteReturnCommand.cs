using MediatR;

namespace CommerceFlow.Application.Returns.Commands.CompleteReturn;

// Return fully processed. The refund itself goes through Payments'
// RefundPaymentCommand - money never moves from here (same call as Shipping,
// where the order status is synced but the payment is untouched).
public sealed record CompleteReturnCommand(Guid ReturnId) : IRequest;
