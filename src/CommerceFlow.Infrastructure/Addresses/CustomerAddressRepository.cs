using CommerceFlow.Application.Addresses.Interfaces;
using CommerceFlow.Domain.Entities.Addresses;
using CommerceFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommerceFlow.Infrastructure.Addresses;

internal sealed class CustomerAddressRepository(CommerceFlowDbContext dbContext)
    : ICustomerAddressRepository
{
    public async Task<CustomerAddress?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.CustomerAddresses
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<CustomerAddress?> GetDefaultAsync(Guid customerId, CancellationToken cancellationToken = default)
        => await dbContext.CustomerAddresses
            .FirstOrDefaultAsync(
                a => a.CustomerId == customerId && a.IsDefault,
                cancellationToken);

    public async Task ClearDefaultAsync(
        Guid customerId,
        Guid keepAddressId,
        CancellationToken cancellationToken = default)
        // One immediate statement: it completes before SaveChanges runs, so the
        // new default can never be written while another row still holds 1.
        => await dbContext.CustomerAddresses
            .Where(a => a.CustomerId == customerId
                && a.IsDefault
                && a.Id != keepAddressId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(a => a.IsDefault, false),
                cancellationToken);

    public async Task<IReadOnlyList<CustomerAddress>> GetAllAsync(
        Guid? customerId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
        => await ApplyFilters(dbContext.CustomerAddresses.AsNoTracking(), customerId)
            // the default address first, then most recently touched
            .OrderByDescending(a => a.IsDefault)
            .ThenByDescending(a => a.UpdatedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

    public async Task<int> CountAsync(
        Guid? customerId,
        CancellationToken cancellationToken = default)
        => await ApplyFilters(dbContext.CustomerAddresses.AsNoTracking(), customerId)
            .CountAsync(cancellationToken);

    public async Task AddAsync(CustomerAddress address, CancellationToken cancellationToken = default)
        => await dbContext.CustomerAddresses.AddAsync(address, cancellationToken);

    public void Remove(CustomerAddress address)
        => dbContext.CustomerAddresses.Remove(address);

    private static IQueryable<CustomerAddress> ApplyFilters(
        IQueryable<CustomerAddress> query,
        Guid? customerId)
    {
        if (customerId.HasValue)
            query = query.Where(a => a.CustomerId == customerId.Value);

        return query;
    }
}
