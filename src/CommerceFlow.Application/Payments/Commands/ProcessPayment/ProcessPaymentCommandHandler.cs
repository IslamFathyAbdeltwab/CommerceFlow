using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Orders.Interfaces;
using CommerceFlow.Application.Payments.Gateways;
using CommerceFlow.Application.Payments.Interfaces;
using CommerceFlow.Application.Payments.Queries;
using CommerceFlow.Domain.Entities.Orders;
using CommerceFlow.Domain.Entities.Payments;
using CommerceFlow.Domain.Enums;
using MediatR;

namespace CommerceFlow.Application.Payments.Commands.ProcessPayment;

public sealed class ProcessPaymentCommandHandler(
    IPaymentRepository payments,
    IOrderRepository orders,
    IPaymentGatewayResolver gateways,
    IUnitOfWork unitOfWork) : IRequestHandler<ProcessPaymentCommand, PaymentDto>
{
    public async Task<PaymentDto> Handle(ProcessPaymentCommand request, CancellationToken cancellationToken)
    {
        var payment = await payments.GetByIdAsync(request.PaymentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Payment), request.PaymentId);

        // every pre-check happens BEFORE the gateway call: no money may move
        // for a payment or order that cannot legally be charged
        if (payment.Status != PaymentStatus.Pending)
            throw new ConflictException("Payment has already been processed.");

        var order = await orders.GetByIdAsync(payment.OrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), payment.OrderId);

        if (order.PaymentStatus != OrderPaymentStatus.Unpaid)
            throw new ConflictException("Order payment is already recorded.");

        if (order.Status is not (OrderStatus.Pending or OrderStatus.Confirmed))
            throw new ConflictException("Only pending or confirmed orders can be marked as paid.");

        var gateway = gateways.Resolve(payment.Provider)
            ?? throw new ConflictException("Payment provider is not supported.");

        // Strategy: provider-specific charging lives behind IPaymentGateway;
        // a decline is data (GatewayChargeResult.Failure), not an exception.
        var result = await gateway.ChargeAsync(
            new GatewayChargeRequest(
                payment.Id,
                payment.CustomerId,
                payment.Amount,
                payment.Currency,
                payment.IdempotencyKey),
            cancellationToken);

        if (result.Succeeded)
        {
            payment.MarkSucceeded(result.TransactionId);
            order.MarkAsPaid(); // cross-module sync through IOrderRepository
        }
        else
        {
            payment.MarkFailed(result.FailureReason);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return PaymentMappings.ToDto(payment);
    }
}
