using CommerceFlow.Application.Catalog.Interfaces;
using CommerceFlow.Domain.Entities.Catalog;
using CommerceFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommerceFlow.Infrastructure.Catalog.Categories;

internal sealed class CategoryRepository(CommerceFlowDbContext dbContext) : ICategoryRepository
{
    public async Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.Categories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<Category?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
        => await dbContext.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Slug == slug, cancellationToken);

    public async Task<IReadOnlyList<Category>> GetAllAsync(
        string? searchTerm,
        bool? isActive,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
        => await ApplyFilters(dbContext.Categories.AsNoTracking(), searchTerm, isActive)
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

    public async Task<int> CountAsync(
        string? searchTerm,
        bool? isActive,
        CancellationToken cancellationToken = default)
        => await ApplyFilters(dbContext.Categories.AsNoTracking(), searchTerm, isActive)
            .CountAsync(cancellationToken);

    public async Task<IReadOnlyList<Category>> GetAllActiveAsync(CancellationToken cancellationToken = default)
        => await dbContext.Categories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.Categories.AsNoTracking().AnyAsync(c => c.Id == id, cancellationToken);

    public async Task<bool> AllExistAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
            return true;

        var distinct = ids.Distinct().ToList();
        return await dbContext.Categories.AsNoTracking()
            .CountAsync(c => distinct.Contains(c.Id), cancellationToken) == distinct.Count;
    }

    public async Task<bool> SlugExistsAsync(string slug, Guid? excludeCategoryId = null, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Categories.AsNoTracking().Where(c => c.Slug == slug);
        if (excludeCategoryId.HasValue)
            query = query.Where(c => c.Id != excludeCategoryId.Value);
        return await query.AnyAsync(cancellationToken);
    }

    public async Task<bool> IsDescendantAsync(Guid categoryId, Guid potentialDescendantId, CancellationToken cancellationToken = default)
    {
        if (categoryId == potentialDescendantId)
            return true;

        // Walk up the parent chain from potentialDescendantId looking for categoryId.
        var currentId = potentialDescendantId;
        while (true)
        {
            var parentId = await dbContext.Categories
                .AsNoTracking()
                .Where(c => c.Id == currentId)
                .Select(c => c.ParentCategoryId)
                .FirstOrDefaultAsync(cancellationToken);

            if (parentId is null)
                return false;

            if (parentId.Value == categoryId)
                return true;

            currentId = parentId.Value;
        }
    }

    public async Task AddAsync(Category category, CancellationToken cancellationToken = default)
        => await dbContext.Categories.AddAsync(category, cancellationToken);

    private static IQueryable<Category> ApplyFilters(
        IQueryable<Category> query,
        string? searchTerm,
        bool? isActive)
    {
        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(c => c.Name.Contains(searchTerm) || c.Slug.Contains(searchTerm));

        if (isActive.HasValue)
            query = query.Where(c => c.IsActive == isActive.Value);

        return query;
    }
}
