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

namespace CommerceFlow.Application.Payments.Commands.RefundPayment;

public sealed class RefundPaymentCommandHandler(
    IPaymentRepository payments,
    IOrderRepository orders,
    IPaymentGatewayResolver gateways,
    IUnitOfWork unitOfWork) : IRequestHandler<RefundPaymentCommand, PaymentDto>
{
    public async Task<PaymentDto> Handle(RefundPaymentCommand request, CancellationToken cancellationToken)
    {
        var payment = await payments.GetByIdAsync(request.PaymentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Payment), request.PaymentId);

        // all pre-checks BEFORE the gateway call: a refund moves money too
        if (payment.Status == PaymentStatus.Refunded)
            throw new ConflictException("Payment has already been refunded.");

        if (payment.Status != PaymentStatus.Succeeded)
            throw new ConflictException("Only succeeded payments can be refunded.");

        var order = await orders.GetByIdAsync(payment.OrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), payment.OrderId);

        if (order.PaymentStatus != OrderPaymentStatus.Paid)
            throw new ConflictException("Order payment is not recorded as paid.");

        var gateway = gateways.Resolve(payment.Provider)
            ?? throw new ConflictException("Payment provider is not supported.");

        var result = await gateway.RefundAsync(
            new GatewayRefundRequest(
                payment.Id,
                payment.ProviderTransactionId!, // domain guarantees one on Succeeded
                payment.Amount,
                payment.Currency),
            cancellationToken);

        if (!result.Succeeded)
            throw new ConflictException(result.FailureReason); // provider's reason as the 409 detail

        payment.Refund();
        order.Refund(); // Paid -> Refunded; fulfillment status untouched

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return PaymentMappings.ToDto(payment);
    }
}
