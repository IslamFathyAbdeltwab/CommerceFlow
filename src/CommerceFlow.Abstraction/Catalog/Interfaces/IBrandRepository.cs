using CommerceFlow.Domain.Entities.Catalog;

namespace CommerceFlow.Application.Catalog.Interfaces;

public interface IBrandRepository
{
    Task<Brand?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Brand?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Brand>> GetAllAsync(
        string? searchTerm,
        bool? isActive,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(
        string? searchTerm,
        bool? isActive,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> SlugExistsAsync(string slug, Guid? excludeBrandId = null, CancellationToken cancellationToken = default);

    Task AddAsync(Brand brand, CancellationToken cancellationToken = default);
}
