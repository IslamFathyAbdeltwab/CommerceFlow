using CommerceFlow.Domain.Entities.Payments;
using CommerceFlow.Domain.Enums;

namespace CommerceFlow.Application.Payments.Interfaces;

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    // Idempotency lookup: same order + same key = same operation, so a
    // retried create returns the original payment instead of a second one.
    Task<Payment?> GetByIdempotencyKeyAsync(
        Guid orderId,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Payment>> GetAllAsync(
        Guid? orderId,
        Guid? customerId,
        PaymentStatus? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(
        Guid? orderId,
        Guid? customerId,
        PaymentStatus? status,
        CancellationToken cancellationToken = default);

    Task AddAsync(Payment payment, CancellationToken cancellationToken = default);
}
