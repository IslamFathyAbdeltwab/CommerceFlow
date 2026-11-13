using CommerceFlow.Domain.Entities.Reviews;
using CommerceFlow.Domain.Enums;
using CommerceFlow.Domain.Exceptions;

namespace CommerceFlow.Domain.Tests.Reviews;

public class ProductReviewTests
{
    private static readonly Guid ProductId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();

    private static ProductReview NewReview(
        int rating = 5,
        string comment = "Excellent, would buy again.",
        string? title = "Great",
        Guid? orderId = null) =>
        ProductReview.Create(ProductId, CustomerId, rating, comment, title, orderId);

    private static ProductReview PublishedReview()
    {
        var review = NewReview();
        review.Approve();
        return review;
    }

    private static ProductReview TakenDownReview()
    {
        var review = PublishedReview();
        review.Reject("Abusive language.");
        return review;
    }

    // ---------- Create ----------

    [Fact]
    public void Create_ValidData_CreatesPendingReview()
    {
        var review = NewReview();

        Assert.Equal(ProductId, review.ProductId);
        Assert.Equal(CustomerId, review.CustomerId);
        Assert.Null(review.OrderId);
        Assert.False(review.IsVerifiedPurchase); // not tied to a purchase
        Assert.Equal(5, review.Rating);
        Assert.Equal("Great", review.Title);
        Assert.Equal("Excellent, would buy again.", review.Comment);
        Assert.Equal(ReviewStatus.Pending, review.Status);
        Assert.Null(review.ModerationReason);
        Assert.Null(review.ModeratedAtUtc);
        Assert.Null(review.SellerReply);
        Assert.Null(review.RepliedAtUtc);
        Assert.NotEqual(Guid.Empty, review.Id);
    }

    [Fact]
    public void Create_SetsAuditTimestamps()
    {
        var review = NewReview();

        Assert.NotEqual(default, review.CreatedAtUtc);
        Assert.NotEqual(default, review.UpdatedAtUtc);
        Assert.True(review.UpdatedAtUtc >= review.CreatedAtUtc);
    }

    [Fact]
    public void Create_WithOrderId_MarksVerifiedPurchase()
    {
        var orderId = Guid.NewGuid();

        var review = NewReview(orderId: orderId);

        Assert.Equal(orderId, review.OrderId);
        Assert.True(review.IsVerifiedPurchase); // derived from OrderId only
    }

    [Fact]
    public void Create_EmptyProductId_Throws()
    {
        var exception = Assert.Throws<DomainException>(() =>
            ProductReview.Create(Guid.Empty, CustomerId, 5, "Nice."));

        Assert.Equal("Product ID is required.", exception.Message);
    }

    [Fact]
    public void Create_EmptyCustomerId_Throws()
    {
        var exception = Assert.Throws<DomainException>(() =>
            ProductReview.Create(ProductId, Guid.Empty, 5, "Nice."));

        Assert.Equal("Customer ID is required.", exception.Message);
    }

    [Fact]
    public void Create_EmptyOrderIdProvided_Throws()
    {
        var exception = Assert.Throws<DomainException>(() =>
            ProductReview.Create(ProductId, CustomerId, 5, "Nice.", orderId: Guid.Empty));

        Assert.Equal("Order ID cannot be empty when provided.", exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_RatingBelowMin_Throws(int rating)
    {
        var exception = Assert.Throws<DomainException>(() =>
            NewReview(rating: rating));

        Assert.Equal("Rating must be between 1 and 5.", exception.Message);
    }

    [Theory]
    [InlineData(6)]
    [InlineData(100)]
    public void Create_RatingAboveMax_Throws(int rating)
    {
        var exception = Assert.Throws<DomainException>(() =>
            NewReview(rating: rating));

        Assert.Equal("Rating must be between 1 and 5.", exception.Message);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public void Create_RatingBoundaries_AreValid(int rating)
    {
        Assert.Equal(rating, NewReview(rating: rating).Rating);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_MissingComment_Throws(string comment)
    {
        var exception = Assert.Throws<DomainException>(() =>
            NewReview(comment: comment));

        Assert.Equal("Comment is required.", exception.Message);
    }

    [Fact]
    public void Create_CommentTooLong_Throws()
    {
        var exception = Assert.Throws<DomainException>(() =>
            NewReview(comment: new string('c', ProductReview.CommentMaxLength + 1)));

        Assert.Equal("Comment cannot exceed 1000 characters.", exception.Message);
    }

    [Fact]
    public void Create_TrimsCommentAndTitle()
    {
        var review = NewReview(comment: "  Solid product.  ", title: "  Solid  ");

        Assert.Equal("Solid product.", review.Comment);
        Assert.Equal("Solid", review.Title);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_OptionalTitle_EmptyOrNullBecomesNull(string? title)
    {
        Assert.Null(NewReview(title: title).Title);
    }

    [Fact]
    public void Create_TitleTooLong_Throws()
    {
        var exception = Assert.Throws<DomainException>(() =>
            NewReview(title: new string('t', ProductReview.TitleMaxLength + 1)));

        Assert.Equal("Title cannot exceed 150 characters.", exception.Message);
    }

    // ---------- Approve ----------

    [Fact]
    public void Approve_FromPending_PublishesAndSetsTimestamp()
    {
        var review = NewReview();

        review.Approve();

        Assert.Equal(ReviewStatus.Approved, review.Status);
        Assert.NotNull(review.ModeratedAtUtc);
        Assert.True(review.ModeratedAtUtc >= review.CreatedAtUtc);
        Assert.Null(review.ModerationReason);
    }

    [Fact]
    public void Approve_WhenAlreadyApproved_IsNoOp()
    {
        var review = PublishedReview();
        var moderatedAt = review.ModeratedAtUtc;
        var updatedAt = review.UpdatedAtUtc;

        review.Approve();

        Assert.Equal(ReviewStatus.Approved, review.Status);
        Assert.Equal(moderatedAt, review.ModeratedAtUtc);
        Assert.Equal(updatedAt, review.UpdatedAtUtc); // no change -> no touch
    }

    [Fact]
    public void Approve_FromRejected_ThrowsAndKeepsReason()
    {
        var review = TakenDownReview();

        var exception = Assert.Throws<DomainException>(() => review.Approve());

        Assert.Equal(
            "Product review cannot move from Rejected to Approved.",
            exception.Message);
        Assert.Equal(ReviewStatus.Rejected, review.Status);
        Assert.Equal("Abusive language.", review.ModerationReason); // untouched
    }

    // ---------- Reject ----------

    [Fact]
    public void Reject_FromPending_RejectsAndStoresTrimmedReason()
    {
        var review = NewReview();

        review.Reject("  Spam content.  ");

        Assert.Equal(ReviewStatus.Rejected, review.Status);
        Assert.Equal("Spam content.", review.ModerationReason); // trimmed
        Assert.NotNull(review.ModeratedAtUtc);
        Assert.Null(review.SellerReply); // never published, so no reply
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Reject_MissingReason_ThrowsAndStaysPending(string reason)
    {
        var review = NewReview();

        var exception = Assert.Throws<DomainException>(() => review.Reject(reason));

        Assert.Equal("Moderation reason is required.", exception.Message);
        Assert.Equal(ReviewStatus.Pending, review.Status);
        Assert.Null(review.ModerationReason);
    }

    [Fact]
    public void Reject_ReasonTooLong_Throws()
    {
        var review = NewReview();

        var exception = Assert.Throws<DomainException>(() =>
            review.Reject(new string('r', ProductReview.ModerationReasonMaxLength + 1)));

        Assert.Equal("Moderation reason cannot exceed 500 characters.", exception.Message);
        Assert.Equal(ReviewStatus.Pending, review.Status);
    }

    [Fact]
    public void Reject_WhenAlreadyRejected_IsNoOpAndKeepsOriginalReason()
    {
        var review = TakenDownReview();
        var moderatedAt = review.ModeratedAtUtc;
        var updatedAt = review.UpdatedAtUtc;

        review.Reject("A different later reason");

        Assert.Equal(ReviewStatus.Rejected, review.Status);
        Assert.Equal("Abusive language.", review.ModerationReason); // original kept
        Assert.Equal(moderatedAt, review.ModeratedAtUtc);
        Assert.Equal(updatedAt, review.UpdatedAtUtc); // no change -> no touch
    }

    [Fact]
    public void Reject_FromApproved_TakesDownPublishedReview()
    {
        var review = PublishedReview();

        review.Reject("Policy violation found later.");

        Assert.Equal(ReviewStatus.Rejected, review.Status);
        Assert.Equal("Policy violation found later.", review.ModerationReason);
        Assert.NotNull(review.ModeratedAtUtc);
        // content itself survives for the audit trail
        Assert.Equal(5, review.Rating);
        Assert.Equal("Excellent, would buy again.", review.Comment);
    }

    // ---------- Reply ----------

    [Fact]
    public void Reply_OnApprovedReview_StoresTrimmedReplyAndTimestamp()
    {
        var review = PublishedReview();

        review.Reply("  Thanks for your feedback!  ");

        Assert.Equal("Thanks for your feedback!", review.SellerReply);
        Assert.NotNull(review.RepliedAtUtc);
        Assert.True(review.RepliedAtUtc >= review.ModeratedAtUtc);
        Assert.Equal(ReviewStatus.Approved, review.Status); // reply never moderates
    }

    [Fact]
    public void Reply_SecondReply_Overwrites()
    {
        var review = PublishedReview();
        review.Reply("First answer.");
        var firstRepliedAt = review.RepliedAtUtc;

        review.Reply("Updated answer.");

        Assert.Equal("Updated answer.", review.SellerReply);
        Assert.True(review.RepliedAtUtc >= firstRepliedAt);
    }

    [Fact]
    public void Reply_WhenPending_ThrowsAndLeavesReviewUntouched()
    {
        var review = NewReview();

        var exception = Assert.Throws<DomainException>(() =>
            review.Reply("Thanks!"));

        Assert.Equal(
            "Only approved reviews can receive a reply.",
            exception.Message);
        Assert.Equal(ReviewStatus.Pending, review.Status); // moderation untouched
        Assert.Null(review.SellerReply);
        Assert.Null(review.RepliedAtUtc);
    }

    [Fact]
    public void Reply_WhenTakenDown_Throws()
    {
        var review = TakenDownReview();

        var exception = Assert.Throws<DomainException>(() =>
            review.Reply("Thanks!"));

        Assert.Equal(
            "Only approved reviews can receive a reply.",
            exception.Message);
        Assert.Null(review.SellerReply);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Reply_MissingReplyText_Throws(string reply)
    {
        var review = PublishedReview();

        var exception = Assert.Throws<DomainException>(() => review.Reply(reply));

        Assert.Equal("Seller reply is required.", exception.Message);
        Assert.Null(review.SellerReply);
        Assert.Null(review.RepliedAtUtc);
    }

    [Fact]
    public void Reply_ReplyTooLong_Throws()
    {
        var review = PublishedReview();

        var exception = Assert.Throws<DomainException>(() =>
            review.Reply(new string('x', ProductReview.SellerReplyMaxLength + 1)));

        Assert.Equal("Seller reply cannot exceed 500 characters.", exception.Message);
        Assert.Null(review.SellerReply);
    }

    // ---------- Terminal state ----------

    [Fact]
    public void TakenDownReview_FurtherModerationBlocked()
    {
        var review = TakenDownReview();

        Assert.Throws<DomainException>(() => review.Approve()); // terminal
        Assert.Equal(ReviewStatus.Rejected, review.Status);
        Assert.Equal("Abusive language.", review.ModerationReason);
        Assert.Null(review.SellerReply); // takedown never invented a reply
    }
}
