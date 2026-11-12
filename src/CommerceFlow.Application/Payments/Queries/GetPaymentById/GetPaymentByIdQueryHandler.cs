using CommerceFlow.Application.Payments.Interfaces;
using MediatR;

namespace CommerceFlow.Application.Payments.Queries.GetPaymentById;

public sealed class GetPaymentByIdQueryHandler(IPaymentRepository payments)
    : IRequestHandler<GetPaymentByIdQuery, PaymentDto?>
{
    public async Task<PaymentDto?> Handle(GetPaymentByIdQuery request, CancellationToken cancellationToken)
    {
        var payment = await payments.GetByIdAsync(request.PaymentId, cancellationToken);

        return payment is null ? null : PaymentMappings.ToDto(payment);
    }
}
