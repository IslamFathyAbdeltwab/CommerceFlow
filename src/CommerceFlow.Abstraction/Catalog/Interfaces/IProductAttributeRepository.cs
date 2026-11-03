using CommerceFlow.Domain.Entities.Catalog;

namespace CommerceFlow.Application.Catalog.Interfaces;

public interface IProductAttributeRepository
{
    Task<ProductAttribute?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> CodeExistsAsync(string code, Guid? excludeAttributeId = null, CancellationToken cancellationToken = default);
    Task AddAsync(ProductAttribute attribute, CancellationToken cancellationToken = default);
}