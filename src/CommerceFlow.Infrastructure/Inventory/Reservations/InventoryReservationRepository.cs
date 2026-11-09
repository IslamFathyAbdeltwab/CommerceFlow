using CommerceFlow.Application.Inventory.Interfaces;
using CommerceFlow.Domain.Entities.Inventory;
using CommerceFlow.Domain.Enums;
using CommerceFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommerceFlow.Infrastructure.Inventory.Reservations;

internal sealed class InventoryReservationRepository(CommerceFlowDbContext dbContext) : IInventoryReservationRepository
{
    public async Task<InventoryReservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.InventoryReservations
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<InventoryReservation?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
        => await dbContext.InventoryReservations
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.OrderId == orderId, cancellationToken);

    public async Task<IReadOnlyList<InventoryReservation>> GetAllAsync(
        Guid? orderId,
        InventoryReservationStatus? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
        => await ApplyFilters(dbContext.InventoryReservations.AsNoTracking(), orderId, status)
            .OrderByDescending(r => r.CreatedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

    public async Task<int> CountAsync(
        Guid? orderId,
        InventoryReservationStatus? status,
        CancellationToken cancellationToken = default)
        => await ApplyFilters(dbContext.InventoryReservations.AsNoTracking(), orderId, status)
            .CountAsync(cancellationToken);

    public async Task AddAsync(InventoryReservation reservation, CancellationToken cancellationToken = default)
        => await dbContext.InventoryReservations.AddAsync(reservation, cancellationToken);

    private static IQueryable<InventoryReservation> ApplyFilters(
        IQueryable<InventoryReservation> query,
        Guid? orderId,
        InventoryReservationStatus? status)
    {
        if (orderId.HasValue)
            query = query.Where(r => r.OrderId == orderId.Value);

        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        return query;
    }
}
