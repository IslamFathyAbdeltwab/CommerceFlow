using CommerceFlow.Application.Reviews.Interfaces;
using MediatR;

namespace CommerceFlow.Application.Reviews.Queries.GetReviewById;

public sealed class GetReviewByIdQueryHandler(IProductReviewRepository reviews)
    : IRequestHandler<GetReviewByIdQuery, ProductReviewDto?>
{
    public async Task<ProductReviewDto?> Handle(GetReviewByIdQuery request, CancellationToken cancellationToken)
    {
        var review = await reviews.GetByIdAsync(request.ReviewId, cancellationToken);

        return review is null ? null : ProductReviewMappings.ToDto(review);
    }
}
