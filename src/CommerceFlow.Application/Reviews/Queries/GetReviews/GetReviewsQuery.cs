using CommerceFlow.Application.Catalog.Products.Queries;
using CommerceFlow.Domain.Enums;
using MediatR;

namespace CommerceFlow.Application.Reviews.Queries.GetReviews;

// The product page reads with ?productId=...&status=Approved; moderation and
// "my reviews" reuse the same query with their own filters.
public sealed record GetReviewsQuery(
    Guid? ProductId = null,
    Guid? CustomerId = null,
    ReviewStatus? Status = null,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PagedResult<ProductReviewDto>>;
