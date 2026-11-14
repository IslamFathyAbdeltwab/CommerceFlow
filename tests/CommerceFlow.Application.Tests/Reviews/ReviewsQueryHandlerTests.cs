using CommerceFlow.Application.Reviews.Interfaces;
using CommerceFlow.Application.Reviews.Queries.GetReviewById;
using CommerceFlow.Application.Reviews.Queries.GetReviews;
using CommerceFlow.Domain.Entities.Reviews;
using CommerceFlow.Domain.Enums;
using Moq;

namespace CommerceFlow.Application.Tests.Reviews;

public sealed class ReviewsQueryHandlerTests
{
    private readonly Mock<IProductReviewRepository> _reviews = new();

    private void SetupReview(ProductReview review) =>
        _reviews
            .Setup(x => x.GetByIdAsync(review.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(review);

    // ---------- GetReviewById ----------

    [Fact]
    public async Task GetReviewById_WhenReviewDoesNotExist_ReturnsNull()
    {
        var handler = new GetReviewByIdQueryHandler(_reviews.Object);

        var result = await handler.Handle(
            new GetReviewByIdQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetReviewById_WhenApprovedVerifiedWithReply_MapsAllFields()
    {
        var handler = new GetReviewByIdQueryHandler(_reviews.Object);
        var review = ReviewsFixtures.CreateReview(
            status: ReviewStatus.Approved,
            orderId: Guid.NewGuid(),
            sellerReply: "Thanks for your feedback!");
        SetupReview(review);

        var dto = await handler.Handle(
            new GetReviewByIdQuery(review.Id), CancellationToken.None);

        Assert.NotNull(dto);
        Assert.Equal(review.Id, dto.Id);
        Assert.Equal(ReviewsFixtures.ProductId, dto.ProductId);
        Assert.Equal(review.CustomerId, dto.CustomerId);
        Assert.Equal(review.OrderId, dto.OrderId);
        Assert.True(dto.IsVerifiedPurchase); // derived from OrderId
        Assert.Equal(5, dto.Rating);
        Assert.Equal("Great", dto.Title);
        Assert.Equal("Excellent, would buy again.", dto.Comment);
        Assert.Equal(ReviewStatus.Approved, dto.Status);
        Assert.Null(dto.ModerationReason);
        Assert.NotNull(dto.ModeratedAtUtc);
        Assert.Equal("Thanks for your feedback!", dto.SellerReply);
        Assert.NotNull(dto.RepliedAtUtc);
        Assert.Equal(review.CreatedAtUtc, dto.CreatedAtUtc);
        Assert.Equal(review.UpdatedAtUtc, dto.UpdatedAtUtc);
    }

    [Fact]
    public async Task GetReviewById_WhenPendingAndUnverified_MapsDerivedBadge()
    {
        var handler = new GetReviewByIdQueryHandler(_reviews.Object);
        var review = ReviewsFixtures.CreateReview(status: ReviewStatus.Pending);
        SetupReview(review);

        var dto = await handler.Handle(
            new GetReviewByIdQuery(review.Id), CancellationToken.None);

        Assert.NotNull(dto);
        Assert.Equal(ReviewStatus.Pending, dto.Status);
        Assert.Null(dto.OrderId);
        Assert.False(dto.IsVerifiedPurchase); // no order -> no badge
        Assert.Null(dto.SellerReply);
        Assert.Null(dto.ModeratedAtUtc);
    }

    [Fact]
    public async Task GetReviewById_WhenRejected_MapsModerationReason()
    {
        var handler = new GetReviewByIdQueryHandler(_reviews.Object);
        var review = ReviewsFixtures.CreateReview(
            status: ReviewStatus.Rejected, moderationReason: "Spam content.");
        SetupReview(review);

        var dto = await handler.Handle(
            new GetReviewByIdQuery(review.Id), CancellationToken.None);

        Assert.NotNull(dto);
        Assert.Equal(ReviewStatus.Rejected, dto.Status);
        Assert.Equal("Spam content.", dto.ModerationReason);
        Assert.NotNull(dto.ModeratedAtUtc);
        Assert.Null(dto.SellerReply);
    }

    // ---------- GetReviews ----------

    [Fact]
    public async Task GetReviews_WhenNoneExist_ReturnsEmptyPagedResult()
    {
        var handler = new GetReviewsQueryHandler(_reviews.Object);
        _reviews
            .Setup(x => x.CountAsync(
                It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<ReviewStatus?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _reviews
            .Setup(x => x.GetAllAsync(
                It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<ReviewStatus?>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductReview>());

        var result = await handler.Handle(new GetReviewsQuery(), CancellationToken.None);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(20, result.PageSize);
        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    public async Task GetReviews_WithFilters_PassesThemThroughAndMapsItems()
    {
        var handler = new GetReviewsQueryHandler(_reviews.Object);
        var productId = Guid.NewGuid();
        var first = ReviewsFixtures.CreateReview(
            productId: productId, status: ReviewStatus.Approved);
        var second = ReviewsFixtures.CreateReview(
            productId: productId, status: ReviewStatus.Approved, rating: 3);
        _reviews
            .Setup(x => x.CountAsync(
                productId, null, ReviewStatus.Approved, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        _reviews
            .Setup(x => x.GetAllAsync(
                productId, null, ReviewStatus.Approved, 2, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductReview> { first, second });

        var result = await handler.Handle(
            new GetReviewsQuery(
                ProductId: productId, Status: ReviewStatus.Approved, PageNumber: 2, PageSize: 5),
            CancellationToken.None);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(5, result.PageSize);
        Assert.Equal(
            new[] { first.Id, second.Id },
            result.Items.Select(r => r.Id));
        Assert.Equal(
            new[] { 5, 3 },
            result.Items.Select(r => r.Rating));

        _reviews.Verify(
            x => x.CountAsync(
                productId, null, ReviewStatus.Approved, It.IsAny<CancellationToken>()),
            Times.Once);
        _reviews.Verify(
            x => x.GetAllAsync(
                productId, null, ReviewStatus.Approved, 2, 5, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
