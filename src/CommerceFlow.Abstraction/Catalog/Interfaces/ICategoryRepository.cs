using CommerceFlow.Domain.Entities.Catalog;

namespace CommerceFlow.Application.Catalog.Interfaces;

public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> AllExistAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);
    Task<bool> SlugExistsAsync(string slug, Guid? excludeCategoryId = null, CancellationToken cancellationToken = default);

    // True if potentialDescendantId sits anywhere below categoryId in the tree.
    // Used to block loops (A → B → A) when changing a parent.
    Task<bool> IsDescendantAsync(Guid categoryId, Guid potentialDescendantId, CancellationToken cancellationToken = default);

    Task AddAsync(Category category, CancellationToken cancellationToken = default);
}