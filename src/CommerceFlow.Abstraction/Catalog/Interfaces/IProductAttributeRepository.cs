using CommerceFlow.Domain.Entities.Catalog;

namespace CommerceFlow.Application.Catalog.Interfaces;

public interface IProductAttributeRepository
{
    Task<ProductAttribute?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductAttribute>> GetAllAsync(
        string? searchTerm,
        bool? isVariantDefining,
        bool? isFilterable,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(
        string? searchTerm,
        bool? isVariantDefining,
        bool? isFilterable,
        CancellationToken cancellationToken = default);

    Task<bool> CodeExistsAsync(string code, Guid? excludeAttributeId = null, CancellationToken cancellationToken = default);
    Task AddAsync(ProductAttribute attribute, CancellationToken cancellationToken = default);
}
