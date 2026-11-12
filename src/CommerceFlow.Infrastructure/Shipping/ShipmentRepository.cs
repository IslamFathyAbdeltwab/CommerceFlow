using CommerceFlow.Application.Shipping.Interfaces;
using CommerceFlow.Domain.Entities.Shipping;
using CommerceFlow.Domain.Enums;
using CommerceFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommerceFlow.Infrastructure.Shipping;

internal sealed class ShipmentRepository(CommerceFlowDbContext dbContext)
    : IShipmentRepository
{
    public async Task<Shipment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        // Items always ride along: the detail read maps them and MarkShipped
        // counts the box - an unloaded collection would look empty.
        => await dbContext.Shipments
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<Shipment?> GetByTrackingNumberAsync(
        string trackingNumber,
        CancellationToken cancellationToken = default)
        // Backed by the unique TrackingNumber index: the create pre-check.
        => await dbContext.Shipments
            .FirstOrDefaultAsync(s => s.TrackingNumber == trackingNumber, cancellationToken);

    public async Task<IReadOnlyList<Shipment>> GetByOrderIdAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
        // Items included: the create handler sums them per variant for the
        // unshipped-quantity pre-check across partial shipments.
        => await dbContext.Shipments
            .Include(s => s.Items)
            .Where(s => s.OrderId == orderId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Shipment>> GetAllAsync(
        Guid? orderId,
        ShipmentStatus? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
        => await ApplyFilters(dbContext.Shipments.AsNoTracking().Include(s => s.Items), orderId, status)
            .OrderByDescending(s => s.CreatedAtUtc)
            .ThenByDescending(s => s.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

    public async Task<int> CountAsync(
        Guid? orderId,
        ShipmentStatus? status,
        CancellationToken cancellationToken = default)
        => await ApplyFilters(dbContext.Shipments.AsNoTracking(), orderId, status)
            .CountAsync(cancellationToken);

    public async Task AddAsync(Shipment shipment, CancellationToken cancellationToken = default)
        => await dbContext.Shipments.AddAsync(shipment, cancellationToken);

    private static IQueryable<Shipment> ApplyFilters(
        IQueryable<Shipment> query,
        Guid? orderId,
        ShipmentStatus? status)
    {
        if (orderId.HasValue)
            query = query.Where(s => s.OrderId == orderId.Value);

        if (status.HasValue)
            query = query.Where(s => s.Status == status.Value);

        return query;
    }
}
