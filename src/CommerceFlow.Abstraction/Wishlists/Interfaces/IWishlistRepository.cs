using CommerceFlow.Domain.Entities.Wishlists;

namespace CommerceFlow.Application.Wishlists.Interfaces;

public interface IWishlistRepository
{
    Task<Wishlist?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Wishlist?> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Wishlist>> GetAllAsync(
        Guid? customerId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(
        Guid? customerId,
        CancellationToken cancellationToken = default);

    Task AddAsync(Wishlist wishlist, CancellationToken cancellationToken = default);
}
