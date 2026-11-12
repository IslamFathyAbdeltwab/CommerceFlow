using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Orders.Interfaces;
using CommerceFlow.Application.Payments.Gateways;
using CommerceFlow.Application.Payments.Interfaces;
using CommerceFlow.Domain.Entities.Orders;
using CommerceFlow.Domain.Entities.Payments;
using CommerceFlow.Domain.Enums;
using MediatR;

namespace CommerceFlow.Application.Payments.Commands.CreatePayment;

public sealed class CreatePaymentCommandHandler(
    IPaymentRepository payments,
    IOrderRepository orders,
    IPaymentGatewayResolver gateways,
    IUnitOfWork unitOfWork) : IRequestHandler<CreatePaymentCommand, Guid>
{
    public async Task<Guid> Handle(CreatePaymentCommand request, CancellationToken cancellationToken)
    {
        var idempotencyKey = request.IdempotencyKey.Trim();
        var currency = request.Currency.Trim().ToUpperInvariant();
        var provider = request.Provider.Trim();

        // Idempotent replay comes first: a retried request must return the
        // original payment even if the order moved on (e.g. it got paid).
        var replay = await payments.GetByIdempotencyKeyAsync(
            request.OrderId, idempotencyKey, cancellationToken);

        if (replay is not null)
        {
            // same key + different payload = client bug, not a replay
            if (replay.Amount != request.Amount
                || replay.Currency != currency
                || !string.Equals(replay.Provider, provider, StringComparison.OrdinalIgnoreCase))
                throw new ConflictException(
                    "Idempotency key was already used with a different request.");

            return replay.Id;
        }

        var order = await orders.GetByIdAsync(request.OrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);

        // pre-checks before anything is created (state conflicts -> 409)
        if (order.PaymentStatus != OrderPaymentStatus.Unpaid)
            throw new ConflictException("Order payment is already recorded.");

        if (order.Status is not (OrderStatus.Pending or OrderStatus.Confirmed))
            throw new ConflictException("Only pending or confirmed orders can be marked as paid.");

        if (order.GrandTotal != request.Amount)
            throw new ConflictException("Payment amount must match the order total.");

        if (order.Currency != currency)
            throw new ConflictException("Payment currency must match the order currency.");

        if (gateways.Resolve(provider) is null)
            throw new ConflictException("Payment provider is not supported.");

        var payment = Payment.Create(
            order.Id,
            order.CustomerId,
            request.Amount,
            currency,
            provider,
            idempotencyKey);

        await payments.AddAsync(payment, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return payment.Id;
    }
}
