using CommerceFlow.Domain.Entities.Catalog;

namespace CommerceFlow.Application.Catalog.Interfaces;

public interface IProductRepository
{
    // Loads the whole aggregate (variants, attribute values, images, categories) so it can be changed.
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    // Read-side projections. These stay out of the write-model aggregate.
    Task<IReadOnlyList<Product>> GetPublishedProductsAsync(
        string? searchTerm,
        Guid? brandId,
        Guid? categoryId,
        bool? isFeatured,
        decimal? minPrice,
        decimal? maxPrice,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> CountPublishedProductsAsync(
        string? searchTerm,
        Guid? brandId,
        Guid? categoryId,
        bool? isFeatured,
        decimal? minPrice,
        decimal? maxPrice,
        CancellationToken cancellationToken = default);

    Task<Product?> GetPublishedProductByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Product?> GetPublishedProductBySlugAsync(string slug, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Product>> GetFeaturedProductsAsync(
        int take,
        CancellationToken cancellationToken = default);

    Task<int> CountByStatusAsync(CommerceFlow.Domain.Enums.ProductStatus status, CancellationToken cancellationToken = default);

    Task<int> CountPublishedAsync(CancellationToken cancellationToken = default);

    Task<bool> SlugExistsAsync(string slug, Guid? excludeProductId = null, CancellationToken cancellationToken = default);
    Task<bool> SkuExistsAsync(string sku, Guid? excludeVariantId = null, CancellationToken cancellationToken = default);

    Task AddAsync(Product product, CancellationToken cancellationToken = default);
}