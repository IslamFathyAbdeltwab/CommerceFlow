using CommerceFlow.Domain.Entities.Shipping;
using CommerceFlow.Domain.Enums;

namespace CommerceFlow.Application.Shipping.Interfaces;

public interface IShipmentRepository
{
    Task<Shipment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    // Tracking numbers are unique: two shipments with the same number would
    // make a carrier lookup ambiguous, so create pre-checks against this.
    Task<Shipment?> GetByTrackingNumberAsync(
        string trackingNumber,
        CancellationToken cancellationToken = default);

    // Every shipment of one order: the create handler sums these to enforce
    // that partial shipments never exceed the ordered quantities.
    Task<IReadOnlyList<Shipment>> GetByOrderIdAsync(
        Guid orderId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Shipment>> GetAllAsync(
        Guid? orderId,
        ShipmentStatus? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(
        Guid? orderId,
        ShipmentStatus? status,
        CancellationToken cancellationToken = default);

    Task AddAsync(Shipment shipment, CancellationToken cancellationToken = default);
}
