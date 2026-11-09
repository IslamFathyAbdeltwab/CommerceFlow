using CommerceFlow.Application.Catalog.Interfaces;
using CommerceFlow.Domain.Entities.Catalog;
using CommerceFlow.Domain.Enums;
using CommerceFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommerceFlow.Infrastructure.Catalog.Products;

// Repository pattern: loads/saves the Product aggregate through EF Core.
// Queries (reads) use AsNoTracking; GetByIdAsync is tracked for changes.
internal sealed class ProductRepository(CommerceFlowDbContext dbContext) : IProductRepository
{
    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.Products
            .Include(p => p.Variants)
                .ThenInclude(v => v.AttributeValues)
            .Include(p => p.Images)
            .Include(p => p.Categories)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Product>> GetPublishedProductsAsync(
        string? searchTerm,
        Guid? brandId,
        Guid? categoryId,
        bool? isFeatured,
        decimal? minPrice,
        decimal? maxPrice,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyPublishedFilters(
            dbContext.Products.AsNoTracking(),
            searchTerm,
            brandId,
            categoryId,
            isFeatured,
            minPrice,
            maxPrice);

        return await query
            .Include(p => p.Variants)
                .ThenInclude(v => v.AttributeValues)
            .Include(p => p.Images)
            .Include(p => p.Categories)
            .OrderByDescending(p => p.PublishedAtUtc)
            .ThenBy(p => p.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountPublishedProductsAsync(
        string? searchTerm,
        Guid? brandId,
        Guid? categoryId,
        bool? isFeatured,
        decimal? minPrice,
        decimal? maxPrice,
        CancellationToken cancellationToken = default)
        => await ApplyPublishedFilters(
            dbContext.Products.AsNoTracking(),
            searchTerm,
            brandId,
            categoryId,
            isFeatured,
            minPrice,
            maxPrice)
            .CountAsync(cancellationToken);

    public async Task<Product?> GetProductByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.Products
            .AsNoTracking()
            .Include(p => p.Variants)
                .ThenInclude(v => v.AttributeValues)
            .Include(p => p.Images)
            .Include(p => p.Categories)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<Product?> GetProductBySlugAsync(string slug, CancellationToken cancellationToken = default)
        => await dbContext.Products
            .AsNoTracking()
            .Include(p => p.Variants)
                .ThenInclude(v => v.AttributeValues)
            .Include(p => p.Images)
            .Include(p => p.Categories)
            .FirstOrDefaultAsync(p => p.Slug == slug, cancellationToken);

    public async Task<Product?> GetPublishedProductByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.Products
            .AsNoTracking()
            .Include(p => p.Variants)
                .ThenInclude(v => v.AttributeValues)
            .Include(p => p.Images)
            .Include(p => p.Categories)
            .FirstOrDefaultAsync(p => p.Id == id && p.Status == ProductStatus.Published, cancellationToken);

    public async Task<Product?> GetPublishedProductBySlugAsync(string slug, CancellationToken cancellationToken = default)
        => await dbContext.Products
            .AsNoTracking()
            .Include(p => p.Variants)
                .ThenInclude(v => v.AttributeValues)
            .Include(p => p.Images)
            .Include(p => p.Categories)
            .FirstOrDefaultAsync(p => p.Slug == slug && p.Status == ProductStatus.Published, cancellationToken);

    public async Task<IReadOnlyList<Product>> GetFeaturedProductsAsync(
        int take,
        CancellationToken cancellationToken = default)
        => await dbContext.Products
            .AsNoTracking()
            .Include(p => p.Variants)
                .ThenInclude(v => v.AttributeValues)
            .Include(p => p.Images)
            .Include(p => p.Categories)
            .Where(p => p.Status == ProductStatus.Published && p.IsFeatured)
            .OrderByDescending(p => p.PublishedAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken);

    public async Task<int> CountByStatusAsync(ProductStatus status, CancellationToken cancellationToken = default)
        => await dbContext.Products.AsNoTracking()
            .CountAsync(p => p.Status == status, cancellationToken);

    public async Task<int> CountPublishedAsync(CancellationToken cancellationToken = default)
        => await dbContext.Products.AsNoTracking()
            .CountAsync(p => p.Status == ProductStatus.Published, cancellationToken);

    public async Task<bool> SlugExistsAsync(string slug, Guid? excludeProductId = null, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Products.AsNoTracking().Where(p => p.Slug == slug);
        if (excludeProductId.HasValue)
            query = query.Where(p => p.Id != excludeProductId.Value);
        return await query.AnyAsync(cancellationToken);
    }

    public async Task<bool> SkuExistsAsync(string sku, Guid? excludeVariantId = null, CancellationToken cancellationToken = default)
    {
        var query = dbContext.ProductVariants.AsNoTracking().Where(v => v.Sku == sku);
        if (excludeVariantId.HasValue)
            query = query.Where(v => v.Id != excludeVariantId.Value);
        return await query.AnyAsync(cancellationToken);
    }

    public async Task AddAsync(Product product, CancellationToken cancellationToken = default)
        => await dbContext.Products.AddAsync(product, cancellationToken);

    private static IQueryable<Product> ApplyPublishedFilters(
        IQueryable<Product> query,
        string? searchTerm,
        Guid? brandId,
        Guid? categoryId,
        bool? isFeatured,
        decimal? minPrice,
        decimal? maxPrice)
    {
        query = query.Where(p => p.Status == ProductStatus.Published);

        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(p => p.Name.Contains(searchTerm) || p.Description.Contains(searchTerm));

        if (brandId.HasValue)
            query = query.Where(p => p.BrandId == brandId.Value);

        if (categoryId.HasValue)
            query = query.Where(p => p.Categories.Any(c => c.CategoryId == categoryId.Value));

        if (isFeatured.HasValue)
            query = query.Where(p => p.IsFeatured == isFeatured.Value);

        if (minPrice.HasValue)
            query = query.Where(p => p.Variants.Any(v => v.IsActive && v.Price >= minPrice.Value));

        if (maxPrice.HasValue)
            query = query.Where(p => p.Variants.Any(v => v.IsActive && v.Price <= maxPrice.Value));

        return query;
    }
}
