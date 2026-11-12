using CommerceFlow.Application.Catalog.Products.Queries;
using CommerceFlow.Domain.Enums;
using MediatR;

namespace CommerceFlow.Application.Payments.Queries.GetPayments;

public sealed record GetPaymentsQuery(
    Guid? OrderId = null,
    Guid? CustomerId = null,
    PaymentStatus? Status = null,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PagedResult<PaymentDto>>;
