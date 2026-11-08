using CommerceFlow.Application.Catalog.Interfaces;
using CommerceFlow.Domain.Entities.Catalog;
using CommerceFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommerceFlow.Infrastructure.Catalog.Brands;

internal sealed class BrandRepository(CommerceFlowDbContext dbContext) : IBrandRepository
{
    public async Task<Brand?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.Brands.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public async Task<Brand?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
        => await dbContext.Brands
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Slug == slug, cancellationToken);

    public async Task<IReadOnlyList<Brand>> GetAllAsync(
        string? searchTerm,
        bool? isActive,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
        => await ApplyFilters(dbContext.Brands.AsNoTracking(), searchTerm, isActive)
            .OrderBy(b => b.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

    public async Task<int> CountAsync(
        string? searchTerm,
        bool? isActive,
        CancellationToken cancellationToken = default)
        => await ApplyFilters(dbContext.Brands.AsNoTracking(), searchTerm, isActive)
            .CountAsync(cancellationToken);

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.Brands.AsNoTracking().AnyAsync(b => b.Id == id, cancellationToken);

    public async Task<bool> SlugExistsAsync(string slug, Guid? excludeBrandId = null, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Brands.AsNoTracking().Where(b => b.Slug == slug);
        if (excludeBrandId.HasValue)
            query = query.Where(b => b.Id != excludeBrandId.Value);
        return await query.AnyAsync(cancellationToken);
    }

    public async Task AddAsync(Brand brand, CancellationToken cancellationToken = default)
        => await dbContext.Brands.AddAsync(brand, cancellationToken);

    private static IQueryable<Brand> ApplyFilters(
        IQueryable<Brand> query,
        string? searchTerm,
        bool? isActive)
    {
        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(b => b.Name.Contains(searchTerm) || b.Slug.Contains(searchTerm));

        if (isActive.HasValue)
            query = query.Where(b => b.IsActive == isActive.Value);

        return query;
    }
}
