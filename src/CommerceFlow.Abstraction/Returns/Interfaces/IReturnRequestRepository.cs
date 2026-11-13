using CommerceFlow.Domain.Entities.Returns;
using CommerceFlow.Domain.Enums;

namespace CommerceFlow.Application.Returns.Interfaces;

public interface IReturnRequestRepository
{
    Task<ReturnRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    // Every return request of one order: the create handler sums these to
    // enforce that returned quantities never exceed the ordered quantities.
    Task<IReadOnlyList<ReturnRequest>> GetByOrderIdAsync(
        Guid orderId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReturnRequest>> GetAllAsync(
        Guid? orderId,
        Guid? customerId,
        ReturnStatus? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(
        Guid? orderId,
        Guid? customerId,
        ReturnStatus? status,
        CancellationToken cancellationToken = default);

    Task AddAsync(ReturnRequest returnRequest, CancellationToken cancellationToken = default);
}
