using CommerceFlow.Application.Reviews.Commands.ApproveReview;
using CommerceFlow.Application.Reviews.Commands.CreateReview;
using CommerceFlow.Application.Reviews.Commands.RejectReview;
using CommerceFlow.Application.Reviews.Commands.ReplyToReview;
using CommerceFlow.Application.Reviews.Queries.GetReviews;
using CommerceFlow.Domain.Entities.Reviews;

namespace CommerceFlow.Application.Tests.Reviews;

public sealed class ReviewsValidatorTests
{
    private static string TooLong(int length) => new('x', length);

    // ---------- CreateReview ----------

    [Fact]
    public void Create_WithValidInput_IsValid()
    {
        var result = new CreateReviewCommandValidator()
            .Validate(ReviewsFixtures.ValidCreateCommand());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Create_WithEmptyProductId_IsInvalid()
    {
        var result = new CreateReviewCommandValidator()
            .Validate(ReviewsFixtures.ValidCreateCommand() with { ProductId = Guid.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(CreateReviewCommand.ProductId));
    }

    [Fact]
    public void Create_WithEmptyCustomerId_IsInvalid()
    {
        var result = new CreateReviewCommandValidator()
            .Validate(ReviewsFixtures.ValidCreateCommand() with { CustomerId = Guid.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(CreateReviewCommand.CustomerId));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    public void Create_WithRatingOutOfRange_IsInvalid(int rating)
    {
        var result = new CreateReviewCommandValidator()
            .Validate(ReviewsFixtures.ValidCreateCommand() with { Rating = rating });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(CreateReviewCommand.Rating));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public void Create_WithRatingBoundaries_IsValid(int rating)
    {
        var result = new CreateReviewCommandValidator()
            .Validate(ReviewsFixtures.ValidCreateCommand() with { Rating = rating });

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyComment_IsInvalid(string comment)
    {
        var result = new CreateReviewCommandValidator()
            .Validate(ReviewsFixtures.ValidCreateCommand() with { Comment = comment });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(CreateReviewCommand.Comment));
    }

    [Fact]
    public void Create_WithTooLongComment_IsInvalid()
    {
        var result = new CreateReviewCommandValidator()
            .Validate(ReviewsFixtures.ValidCreateCommand() with
            {
                Comment = TooLong(ProductReview.CommentMaxLength + 1)
            });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(CreateReviewCommand.Comment));
    }

    [Fact]
    public void Create_WithTooLongTitle_IsInvalid()
    {
        var result = new CreateReviewCommandValidator()
            .Validate(ReviewsFixtures.ValidCreateCommand() with
            {
                Title = TooLong(ProductReview.TitleMaxLength + 1)
            });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(CreateReviewCommand.Title));
    }

    [Fact]
    public void Create_WithNullTitle_IsValid()
    {
        var result = new CreateReviewCommandValidator()
            .Validate(ReviewsFixtures.ValidCreateCommand() with { Title = null });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Create_WithEmptyOrderIdProvided_IsInvalid()
    {
        var result = new CreateReviewCommandValidator()
            .Validate(ReviewsFixtures.ValidCreateCommand() with { OrderId = Guid.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(CreateReviewCommand.OrderId));
    }

    [Fact]
    public void Create_WithNullOrderId_IsValid()
    {
        var result = new CreateReviewCommandValidator()
            .Validate(ReviewsFixtures.ValidCreateCommand() with { OrderId = null });

        Assert.True(result.IsValid);
    }

    // ---------- ApproveReview ----------

    [Fact]
    public void Approve_WithValidInput_IsValid()
    {
        var result = new ApproveReviewCommandValidator()
            .Validate(new ApproveReviewCommand(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Approve_WithEmptyReviewId_IsInvalid()
    {
        var result = new ApproveReviewCommandValidator()
            .Validate(new ApproveReviewCommand(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(ApproveReviewCommand.ReviewId));
    }

    // ---------- RejectReview ----------

    [Fact]
    public void Reject_WithValidInput_IsValid()
    {
        var result = new RejectReviewCommandValidator()
            .Validate(new RejectReviewCommand(Guid.NewGuid(), "Spam content."));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Reject_WithEmptyReviewId_IsInvalid()
    {
        var result = new RejectReviewCommandValidator()
            .Validate(new RejectReviewCommand(Guid.Empty, "Spam content."));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(RejectReviewCommand.ReviewId));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Reject_WithEmptyReason_IsInvalid(string reason)
    {
        var result = new RejectReviewCommandValidator()
            .Validate(new RejectReviewCommand(Guid.NewGuid(), reason));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(RejectReviewCommand.Reason));
    }

    [Fact]
    public void Reject_WithTooLongReason_IsInvalid()
    {
        var result = new RejectReviewCommandValidator()
            .Validate(new RejectReviewCommand(
                Guid.NewGuid(), TooLong(ProductReview.ModerationReasonMaxLength + 1)));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(RejectReviewCommand.Reason));
    }

    // ---------- ReplyToReview ----------

    [Fact]
    public void Reply_WithValidInput_IsValid()
    {
        var result = new ReplyToReviewCommandValidator()
            .Validate(new ReplyToReviewCommand(Guid.NewGuid(), "Thanks!"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Reply_WithEmptyReviewId_IsInvalid()
    {
        var result = new ReplyToReviewCommandValidator()
            .Validate(new ReplyToReviewCommand(Guid.Empty, "Thanks!"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(ReplyToReviewCommand.ReviewId));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Reply_WithEmptyReplyText_IsInvalid(string reply)
    {
        var result = new ReplyToReviewCommandValidator()
            .Validate(new ReplyToReviewCommand(Guid.NewGuid(), reply));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(ReplyToReviewCommand.Reply));
    }

    [Fact]
    public void Reply_WithTooLongReply_IsInvalid()
    {
        var result = new ReplyToReviewCommandValidator()
            .Validate(new ReplyToReviewCommand(
                Guid.NewGuid(), TooLong(ProductReview.SellerReplyMaxLength + 1)));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(ReplyToReviewCommand.Reply));
    }

    // ---------- GetReviews ----------

    [Fact]
    public void GetReviews_WithDefaults_IsValid()
    {
        var result = new GetReviewsQueryValidator().Validate(new GetReviewsQuery());

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1001, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public void GetReviews_WithPagingOutOfRange_IsInvalid(int pageNumber, int pageSize)
    {
        var result = new GetReviewsQueryValidator()
            .Validate(new GetReviewsQuery(PageNumber: pageNumber, PageSize: pageSize));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(GetReviewsQuery.PageNumber)
            || e.PropertyName == nameof(GetReviewsQuery.PageSize));
    }
}
