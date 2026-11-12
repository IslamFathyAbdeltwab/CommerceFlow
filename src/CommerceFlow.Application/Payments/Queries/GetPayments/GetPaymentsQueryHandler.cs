using CommerceFlow.Application.Catalog.Products.Queries;
using CommerceFlow.Application.Payments.Interfaces;
using MediatR;

namespace CommerceFlow.Application.Payments.Queries.GetPayments;

public sealed class GetPaymentsQueryHandler(IPaymentRepository payments)
    : IRequestHandler<GetPaymentsQuery, PagedResult<PaymentDto>>
{
    public async Task<PagedResult<PaymentDto>> Handle(GetPaymentsQuery request, CancellationToken cancellationToken)
    {
        var totalCount = await payments.CountAsync(
            request.OrderId,
            request.CustomerId,
            request.Status,
            cancellationToken);

        var list = await payments.GetAllAsync(
            request.OrderId,
            request.CustomerId,
            request.Status,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var items = list.Select(PaymentMappings.ToDto).ToList();

        return new PagedResult<PaymentDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}
