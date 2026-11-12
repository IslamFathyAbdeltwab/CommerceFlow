using MediatR;

namespace CommerceFlow.Application.Payments.Queries.GetPaymentById;

// null = not found (the controller maps it to 404, same as GetAddressById).
public sealed record GetPaymentByIdQuery(Guid PaymentId) : IRequest<PaymentDto?>;
