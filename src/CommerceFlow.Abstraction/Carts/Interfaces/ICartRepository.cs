using CommerceFlow.Domain.Entities.Carts;
using CommerceFlow.Domain.Enums;

namespace CommerceFlow.Application.Carts.Interfaces;

public interface ICartRepository
{
    Task<Cart?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Cart?> GetActiveByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Cart>> GetAllAsync(
        Guid? customerId,
        CartStatus? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(
        Guid? customerId,
        CartStatus? status,
        CancellationToken cancellationToken = default);

    Task AddAsync(Cart cart, CancellationToken cancellationToken = default);
}
