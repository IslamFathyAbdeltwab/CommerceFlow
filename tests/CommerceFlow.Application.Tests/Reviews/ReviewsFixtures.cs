using CommerceFlow.Application.Reviews.Commands.CreateReview;
using CommerceFlow.Application.Tests.Orders;
using CommerceFlow.Domain.Entities.Reviews;
using CommerceFlow.Domain.Enums;

namespace CommerceFlow.Application.Tests.Reviews;

// Shared factories for building product reviews (and the create command).
// CustomerId defaults to OrderFixtures.CustomerId so a verified-purchase
// command can be paired with OrderFixtures.CreateOrder without extra wiring.
internal static class ReviewsFixtures
{
    public static readonly Guid ProductId = Guid.Parse("77777777-7777-7777-7777-777777777777");
    public static readonly Guid VariantId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public static ProductReview CreateReview(
        Guid? productId = null,
        Guid? customerId = null,
        ReviewStatus status = ReviewStatus.Pending,
        int rating = 5,
        string comment = "Excellent, would buy again.",
        string? title = "Great",
        Guid? orderId = null,
        string? moderationReason = null,
        string? sellerReply = null)
    {
        var review = ProductReview.Create(
            productId ?? ProductId,
            customerId ?? OrderFixtures.CustomerId,
            rating,
            comment,
            title,
            orderId);

        switch (status)
        {
            case ReviewStatus.Pending:
                break;
            case ReviewStatus.Approved:
                review.Approve();
                break;
            case ReviewStatus.Rejected:
                review.Reject(moderationReason ?? "Spam content.");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(status), status, null);
        }

        if (sellerReply is not null)
            review.Reply(sellerReply); // only works on approved reviews

        return review;
    }

    public static CreateReviewCommand ValidCreateCommand(
        Guid? productId = null,
        Guid? customerId = null,
        int rating = 5,
        string comment = "Excellent, would buy again.",
        string? title = "Great",
        Guid? orderId = null) =>
        new(
            productId ?? ProductId,
            customerId ?? OrderFixtures.CustomerId,
            rating,
            comment,
            title,
            orderId);
}
