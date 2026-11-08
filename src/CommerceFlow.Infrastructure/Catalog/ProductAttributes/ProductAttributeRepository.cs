using CommerceFlow.Application.Catalog.Interfaces;
using CommerceFlow.Domain.Entities.Catalog;
using CommerceFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommerceFlow.Infrastructure.Catalog.ProductAttributes;

internal sealed class ProductAttributeRepository(CommerceFlowDbContext dbContext) : IProductAttributeRepository
{
    public async Task<ProductAttribute?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.ProductAttributes.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ProductAttribute>> GetAllAsync(
        string? searchTerm,
        bool? isVariantDefining,
        bool? isFilterable,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
        => await ApplyFilters(dbContext.ProductAttributes.AsNoTracking(), searchTerm, isVariantDefining, isFilterable)
            .OrderBy(a => a.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

    public async Task<int> CountAsync(
        string? searchTerm,
        bool? isVariantDefining,
        bool? isFilterable,
        CancellationToken cancellationToken = default)
        => await ApplyFilters(dbContext.ProductAttributes.AsNoTracking(), searchTerm, isVariantDefining, isFilterable)
            .CountAsync(cancellationToken);

    public async Task<bool> CodeExistsAsync(string code, Guid? excludeAttributeId = null, CancellationToken cancellationToken = default)
    {
        var query = dbContext.ProductAttributes.AsNoTracking().Where(a => a.Code == code);
        if (excludeAttributeId.HasValue)
            query = query.Where(a => a.Id != excludeAttributeId.Value);
        return await query.AnyAsync(cancellationToken);
    }

    public async Task AddAsync(ProductAttribute attribute, CancellationToken cancellationToken = default)
        => await dbContext.ProductAttributes.AddAsync(attribute, cancellationToken);

    private static IQueryable<ProductAttribute> ApplyFilters(
        IQueryable<ProductAttribute> query,
        string? searchTerm,
        bool? isVariantDefining,
        bool? isFilterable)
    {
        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(a => a.Name.Contains(searchTerm) || a.Code.Contains(searchTerm));

        if (isVariantDefining.HasValue)
            query = query.Where(a => a.IsVariantDefining == isVariantDefining.Value);

        if (isFilterable.HasValue)
            query = query.Where(a => a.IsFilterable == isFilterable.Value);

        return query;
    }
}
