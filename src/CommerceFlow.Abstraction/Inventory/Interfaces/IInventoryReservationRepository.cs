using CommerceFlow.Domain.Entities.Inventory;
using CommerceFlow.Domain.Enums;

namespace CommerceFlow.Application.Inventory.Interfaces;

public interface IInventoryReservationRepository
{
    Task<InventoryReservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<InventoryReservation?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InventoryReservation>> GetAllAsync(
        Guid? orderId,
        InventoryReservationStatus? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(
        Guid? orderId,
        InventoryReservationStatus? status,
        CancellationToken cancellationToken = default);

    Task AddAsync(InventoryReservation reservation, CancellationToken cancellationToken = default);
}
