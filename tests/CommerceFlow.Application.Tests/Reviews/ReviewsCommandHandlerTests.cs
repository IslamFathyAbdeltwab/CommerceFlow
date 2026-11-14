using CommerceFlow.Application.Catalog.Interfaces;
using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Orders.Interfaces;
using CommerceFlow.Application.Reviews.Commands.ApproveReview;
using CommerceFlow.Application.Reviews.Commands.CreateReview;
using CommerceFlow.Application.Reviews.Commands.RejectReview;
using CommerceFlow.Application.Reviews.Commands.ReplyToReview;
using CommerceFlow.Application.Reviews.Interfaces;
using CommerceFlow.Application.Tests.Orders;
using CommerceFlow.Domain.Entities.Catalog;
using CommerceFlow.Domain.Entities.Orders;
using CommerceFlow.Domain.Entities.Reviews;
using CommerceFlow.Domain.Enums;
using Moq;

namespace CommerceFlow.Application.Tests.Reviews;

public sealed class ReviewsCommandHandlerTests
{
    private readonly Mock<IProductReviewRepository> _reviews = new();
    private readonly Mock<IProductRepository> _products = new();
    private readonly Mock<IOrderRepository> _orders = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private void SetupProduct(Guid productId) =>
        _products
            .Setup(x => x.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Product.Create("iPhone 15", "iphone-15", "Best phone", null));

    private void SetupProductMissing(Guid productId) =>
        _products
            .Setup(x => x.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

    private void SetupOrder(Guid orderId, Order order) =>
        _orders
            .Setup(x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

    private void SetupOrderMissing(Guid orderId) =>
        _orders
            .Setup(x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

    private void SetupReview(ProductReview review) =>
        _reviews
            .Setup(x => x.GetByIdAsync(review.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(review);

    private void SetupReviewMissing(Guid id) =>
        _reviews
            .Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductReview?)null);

    // every create test that passes the earlier pre-checks needs this: the
    // duplicate check reads the list, and loose Moq would hand null
    private void SetupNoExistingReview() =>
        _reviews
            .Setup(x => x.GetByProductAndCustomerAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductReview?)null);

    private void SetupExistingReview(ProductReview taken) =>
        _reviews
            .Setup(x => x.GetByProductAndCustomerAsync(
                taken.ProductId, taken.CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(taken);

    private static void VerifyNeverSaved(Mock<IUnitOfWork> unitOfWork) =>
        unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

    // ---------- CreateReview ----------

    [Fact]
    public async Task Create_WithValidInput_CreatesPendingReviewAndSaves()
    {
        var handler = new CreateReviewCommandHandler(
            _reviews.Object, _products.Object, _orders.Object, _unitOfWork.Object);
        SetupProduct(ReviewsFixtures.ProductId);
        SetupNoExistingReview();

        var id = await handler.Handle(
            ReviewsFixtures.ValidCreateCommand(
                comment: "  Solid product.  ", title: "  Solid  "),
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, id);
        _reviews.Verify(
            x => x.AddAsync(
                It.Is<ProductReview>(r => r.Id == id
                    && r.ProductId == ReviewsFixtures.ProductId
                    && r.CustomerId == OrderFixtures.CustomerId
                    && r.Rating == 5
                    && r.Comment == "Solid product." // trimmed
                    && r.Title == "Solid" // trimmed
                    && r.OrderId == null
                    && !r.IsVerifiedPurchase
                    && r.Status == ReviewStatus.Pending),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_WithBelongingOrder_MarksVerifiedPurchase()
    {
        var handler = new CreateReviewCommandHandler(
            _reviews.Object, _products.Object, _orders.Object, _unitOfWork.Object);
        SetupProduct(ReviewsFixtures.ProductId);
        SetupNoExistingReview();
        var order = OrderFixtures.CreateOrder(OrderStatus.Delivered);
        SetupOrder(order.Id, order); // customer matches the fixture default

        var id = await handler.Handle(
            ReviewsFixtures.ValidCreateCommand(orderId: order.Id),
            CancellationToken.None);

        _reviews.Verify(
            x => x.AddAsync(
                It.Is<ProductReview>(r => r.Id == id
                    && r.OrderId == order.Id
                    && r.IsVerifiedPurchase), // derived, not stored twice
                It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_WhenProductDoesNotExist_ThrowsNotFound()
    {
        var handler = new CreateReviewCommandHandler(
            _reviews.Object, _products.Object, _orders.Object, _unitOfWork.Object);
        SetupProductMissing(ReviewsFixtures.ProductId);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(ReviewsFixtures.ValidCreateCommand(), CancellationToken.None));

        _reviews.Verify(
            x => x.GetByProductAndCustomerAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _reviews.Verify(
            x => x.AddAsync(It.IsAny<ProductReview>(), It.IsAny<CancellationToken>()),
            Times.Never);
        VerifyNeverSaved(_unitOfWork);
    }

    [Fact]
    public async Task Create_WhenOrderDoesNotExist_ThrowsNotFound()
    {
        var handler = new CreateReviewCommandHandler(
            _reviews.Object, _products.Object, _orders.Object, _unitOfWork.Object);
        SetupProduct(ReviewsFixtures.ProductId);
        var orderId = Guid.NewGuid();
        SetupOrderMissing(orderId);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                ReviewsFixtures.ValidCreateCommand(orderId: orderId),
                CancellationToken.None));

        _reviews.Verify(
            x => x.GetByProductAndCustomerAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        VerifyNeverSaved(_unitOfWork);
    }

    [Fact]
    public async Task Create_WhenOrderBelongsToAnotherCustomer_ThrowsConflict()
    {
        var handler = new CreateReviewCommandHandler(
            _reviews.Object, _products.Object, _orders.Object, _unitOfWork.Object);
        SetupProduct(ReviewsFixtures.ProductId);
        var order = OrderFixtures.CreateOrder(OrderStatus.Delivered);
        SetupOrder(order.Id, order); // owned by OrderFixtures.CustomerId
        var someoneElse = Guid.NewGuid();

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(
                ReviewsFixtures.ValidCreateCommand(
                    customerId: someoneElse, orderId: order.Id),
                CancellationToken.None));

        Assert.Contains("Order does not belong to the customer.", exception.Message);
        _reviews.Verify(
            x => x.GetByProductAndCustomerAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never); // the badge check failed first
        _reviews.Verify(
            x => x.AddAsync(It.IsAny<ProductReview>(), It.IsAny<CancellationToken>()),
            Times.Never);
        VerifyNeverSaved(_unitOfWork);
    }

    [Fact]
    public async Task Create_WhenCustomerAlreadyReviewedProduct_ThrowsConflict()
    {
        var handler = new CreateReviewCommandHandler(
            _reviews.Object, _products.Object, _orders.Object, _unitOfWork.Object);
        SetupProduct(ReviewsFixtures.ProductId);
        SetupExistingReview(ReviewsFixtures.CreateReview());

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(ReviewsFixtures.ValidCreateCommand(), CancellationToken.None));

        Assert.Contains("Customer has already reviewed this product.", exception.Message);
        _reviews.Verify(
            x => x.AddAsync(It.IsAny<ProductReview>(), It.IsAny<CancellationToken>()),
            Times.Never);
        VerifyNeverSaved(_unitOfWork);
    }

    // ---------- ApproveReview ----------

    [Fact]
    public async Task Approve_FromPending_PublishesAndSaves()
    {
        var handler = new ApproveReviewCommandHandler(_reviews.Object, _unitOfWork.Object);
        var review = ReviewsFixtures.CreateReview();
        SetupReview(review);

        await handler.Handle(new ApproveReviewCommand(review.Id), CancellationToken.None);

        Assert.Equal(ReviewStatus.Approved, review.Status);
        Assert.NotNull(review.ModeratedAtUtc);
        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Approve_WhenReviewDoesNotExist_ThrowsNotFound()
    {
        var handler = new ApproveReviewCommandHandler(_reviews.Object, _unitOfWork.Object);
        SetupReviewMissing(Guid.NewGuid());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new ApproveReviewCommand(Guid.NewGuid()), CancellationToken.None));

        VerifyNeverSaved(_unitOfWork);
    }

    [Theory]
    [InlineData(ReviewStatus.Approved)]
    [InlineData(ReviewStatus.Rejected)]
    public async Task Approve_WhenReviewIsNotPending_ThrowsConflict(ReviewStatus status)
    {
        var handler = new ApproveReviewCommandHandler(_reviews.Object, _unitOfWork.Object);
        var review = ReviewsFixtures.CreateReview(status: status);
        SetupReview(review);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new ApproveReviewCommand(review.Id), CancellationToken.None));

        Assert.Contains(
            $"Only pending reviews can be approved (current: {status}).",
            exception.Message);
        Assert.Equal(status, review.Status); // untouched
        VerifyNeverSaved(_unitOfWork);
    }

    // ---------- RejectReview ----------

    [Fact]
    public async Task Reject_FromPending_RejectsWithTrimmedReasonAndSaves()
    {
        var handler = new RejectReviewCommandHandler(_reviews.Object, _unitOfWork.Object);
        var review = ReviewsFixtures.CreateReview();
        SetupReview(review);

        await handler.Handle(
            new RejectReviewCommand(review.Id, "  Spam content.  "),
            CancellationToken.None);

        Assert.Equal(ReviewStatus.Rejected, review.Status);
        Assert.Equal("Spam content.", review.ModerationReason); // trimmed
        Assert.NotNull(review.ModeratedAtUtc);
        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Reject_FromApproved_TakesDownAndSaves()
    {
        var handler = new RejectReviewCommandHandler(_reviews.Object, _unitOfWork.Object);
        var review = ReviewsFixtures.CreateReview(status: ReviewStatus.Approved);
        SetupReview(review);

        await handler.Handle(
            new RejectReviewCommand(review.Id, "Policy violation found later."),
            CancellationToken.None);

        Assert.Equal(ReviewStatus.Rejected, review.Status);
        Assert.Equal("Policy violation found later.", review.ModerationReason);
        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Reject_WhenReviewDoesNotExist_ThrowsNotFound()
    {
        var handler = new RejectReviewCommandHandler(_reviews.Object, _unitOfWork.Object);
        SetupReviewMissing(Guid.NewGuid());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new RejectReviewCommand(Guid.NewGuid(), "Spam."), CancellationToken.None));

        VerifyNeverSaved(_unitOfWork);
    }

    [Fact]
    public async Task Reject_WhenReviewIsRejected_IsTerminalConflict()
    {
        var handler = new RejectReviewCommandHandler(_reviews.Object, _unitOfWork.Object);
        var review = ReviewsFixtures.CreateReview(
            status: ReviewStatus.Rejected, moderationReason: "Spam content.");
        SetupReview(review);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new RejectReviewCommand(review.Id, "A later reason."), CancellationToken.None));

        Assert.Contains(
            "Rejected reviews cannot be moderated again (current: Rejected).",
            exception.Message);
        Assert.Equal(ReviewStatus.Rejected, review.Status);
        Assert.Equal("Spam content.", review.ModerationReason); // original kept
        VerifyNeverSaved(_unitOfWork);
    }

    // ---------- ReplyToReview ----------

    [Fact]
    public async Task Reply_OnApprovedReview_StoresTrimmedReplyAndSaves()
    {
        var handler = new ReplyToReviewCommandHandler(_reviews.Object, _unitOfWork.Object);
        var review = ReviewsFixtures.CreateReview(status: ReviewStatus.Approved);
        SetupReview(review);

        await handler.Handle(
            new ReplyToReviewCommand(review.Id, "  Thanks for your feedback!  "),
            CancellationToken.None);

        Assert.Equal("Thanks for your feedback!", review.SellerReply); // trimmed
        Assert.NotNull(review.RepliedAtUtc);
        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Reply_WhenAlreadyReplied_OverwritesAndSaves()
    {
        var handler = new ReplyToReviewCommandHandler(_reviews.Object, _unitOfWork.Object);
        var review = ReviewsFixtures.CreateReview(
            status: ReviewStatus.Approved, sellerReply: "First answer.");
        SetupReview(review);

        await handler.Handle(
            new ReplyToReviewCommand(review.Id, "Updated answer."), CancellationToken.None);

        Assert.Equal("Updated answer.", review.SellerReply);
        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Reply_WhenReviewDoesNotExist_ThrowsNotFound()
    {
        var handler = new ReplyToReviewCommandHandler(_reviews.Object, _unitOfWork.Object);
        SetupReviewMissing(Guid.NewGuid());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new ReplyToReviewCommand(Guid.NewGuid(), "Thanks!"), CancellationToken.None));

        VerifyNeverSaved(_unitOfWork);
    }

    [Theory]
    [InlineData(ReviewStatus.Pending)]
    [InlineData(ReviewStatus.Rejected)]
    public async Task Reply_WhenReviewIsNotApproved_ThrowsConflict(ReviewStatus status)
    {
        var handler = new ReplyToReviewCommandHandler(_reviews.Object, _unitOfWork.Object);
        var review = ReviewsFixtures.CreateReview(status: status);
        SetupReview(review);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new ReplyToReviewCommand(review.Id, "Thanks!"), CancellationToken.None));

        Assert.Contains(
            $"Only approved reviews can receive a reply (current: {status}).",
            exception.Message);
        Assert.Null(review.SellerReply); // untouched
        VerifyNeverSaved(_unitOfWork);
    }
}
