using CommerceFlow.Domain.Entities.Reviews;
using CommerceFlow.Domain.Enums;

namespace CommerceFlow.Application.Reviews.Interfaces;

public interface IProductReviewRepository
{
    Task<ProductReview?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    // One review per customer per product: the create handler pre-checks this
    // so a second submission answers 409 instead of silently replacing the
    // first (same call as IShipmentRepository.GetByTrackingNumberAsync).
    Task<ProductReview?> GetByProductAndCustomerAsync(
        Guid productId,
        Guid customerId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductReview>> GetAllAsync(
        Guid? productId,
        Guid? customerId,
        ReviewStatus? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(
        Guid? productId,
        Guid? customerId,
        ReviewStatus? status,
        CancellationToken cancellationToken = default);

    Task AddAsync(ProductReview review, CancellationToken cancellationToken = default);
}
