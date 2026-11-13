using CommerceFlow.Application.Catalog.Products.Queries;
using CommerceFlow.Application.Reviews.Interfaces;
using MediatR;

namespace CommerceFlow.Application.Reviews.Queries.GetReviews;

public sealed class GetReviewsQueryHandler(IProductReviewRepository reviews)
    : IRequestHandler<GetReviewsQuery, PagedResult<ProductReviewDto>>
{
    public async Task<PagedResult<ProductReviewDto>> Handle(GetReviewsQuery request, CancellationToken cancellationToken)
    {
        var totalCount = await reviews.CountAsync(
            request.ProductId,
            request.CustomerId,
            request.Status,
            cancellationToken);

        var list = await reviews.GetAllAsync(
            request.ProductId,
            request.CustomerId,
            request.Status,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var items = list.Select(ProductReviewMappings.ToDto).ToList();

        return new PagedResult<ProductReviewDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}
