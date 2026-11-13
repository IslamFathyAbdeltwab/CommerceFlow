using CommerceFlow.Application.Reviews.Interfaces;
using CommerceFlow.Domain.Entities.Reviews;
using CommerceFlow.Domain.Enums;
using CommerceFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommerceFlow.Infrastructure.Reviews;

internal sealed class ProductReviewRepository(CommerceFlowDbContext dbContext)
    : IProductReviewRepository
{
    public async Task<ProductReview?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.ProductReviews
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<ProductReview?> GetByProductAndCustomerAsync(
        Guid productId,
        Guid customerId,
        CancellationToken cancellationToken = default)
        // Backed by the unique (ProductId, CustomerId) index: the create
        // pre-check for one review per customer per product.
        => await dbContext.ProductReviews
            .FirstOrDefaultAsync(
                r => r.ProductId == productId && r.CustomerId == customerId,
                cancellationToken);

    public async Task<IReadOnlyList<ProductReview>> GetAllAsync(
        Guid? productId,
        Guid? customerId,
        ReviewStatus? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
        => await ApplyFilters(dbContext.ProductReviews.AsNoTracking(), productId, customerId, status)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ThenByDescending(r => r.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

    public async Task<int> CountAsync(
        Guid? productId,
        Guid? customerId,
        ReviewStatus? status,
        CancellationToken cancellationToken = default)
        => await ApplyFilters(dbContext.ProductReviews.AsNoTracking(), productId, customerId, status)
            .CountAsync(cancellationToken);

    public async Task AddAsync(ProductReview review, CancellationToken cancellationToken = default)
        => await dbContext.ProductReviews.AddAsync(review, cancellationToken);

    private static IQueryable<ProductReview> ApplyFilters(
        IQueryable<ProductReview> query,
        Guid? productId,
        Guid? customerId,
        ReviewStatus? status)
    {
        if (productId.HasValue)
            query = query.Where(r => r.ProductId == productId.Value);

        if (customerId.HasValue)
            query = query.Where(r => r.CustomerId == customerId.Value);

        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        return query;
    }
}
