using CommerceFlow.Domain.Entities.Addresses;

namespace CommerceFlow.Application.Addresses.Interfaces;

public interface ICustomerAddressRepository
{
    Task<CustomerAddress?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<CustomerAddress?> GetDefaultAsync(Guid customerId, CancellationToken cancellationToken = default);

    // Clears the customer's current default (except keepAddressId) as its own
    // immediate statement, before SaveChanges stages the new default. Both
    // changes in one EF batch would be rejected by SQL Server: it validates
    // the filtered unique index per statement, so the second UPDATE would see
    // (CustomerId, IsDefault=1) twice regardless of EF's statement order.
    Task ClearDefaultAsync(
        Guid customerId,
        Guid keepAddressId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CustomerAddress>> GetAllAsync(
        Guid? customerId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(
        Guid? customerId,
        CancellationToken cancellationToken = default);

    Task AddAsync(CustomerAddress address, CancellationToken cancellationToken = default);

    void Remove(CustomerAddress address);
}
