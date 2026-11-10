using CommerceFlow.Domain.Entities.Orders;
using CommerceFlow.Domain.Enums;

namespace CommerceFlow.Application.Orders.Interfaces;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Order>> GetAllAsync(
        Guid? customerId,
        OrderStatus? status,
        OrderPaymentStatus? paymentStatus,
        string? searchTerm,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(
        Guid? customerId,
        OrderStatus? status,
        OrderPaymentStatus? paymentStatus,
        string? searchTerm,
        CancellationToken cancellationToken = default);

    Task AddAsync(Order order, CancellationToken cancellationToken = default);
}
