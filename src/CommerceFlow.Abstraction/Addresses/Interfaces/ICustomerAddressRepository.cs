using CommerceFlow.Domain.Entities.Addresses;

namespace CommerceFlow.Application.Addresses.Interfaces;

public interface ICustomerAddressRepository
{
    Task<CustomerAddress?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<CustomerAddress?> GetDefaultAsync(Guid customerId, CancellationToken cancellationToken = default);

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
