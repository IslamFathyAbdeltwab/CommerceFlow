using CommerceFlow.Domain.Common;
using CommerceFlow.Domain.Enums;
using CommerceFlow.Domain.Exceptions;

namespace CommerceFlow.Domain.Entities.Reviews;

// Aggregate root: a customer's rating of a product, subject to moderation.
// ProductId/CustomerId are ID-only (Catalog/Identity stay other aggregates,
// same call as Payment). One review per customer per product is enforced by
// the Application layer against the repository, not here. New reviews start
// Pending; only approved reviews are shown and can receive a seller reply.
public class ProductReview : AggregateRoot
{
    public const int RatingMin = 1;
    public const int RatingMax = 5;
    public const int TitleMaxLength = 150;
    public const int CommentMaxLength = 1000;
    public const int ModerationReasonMaxLength = 500;
    public const int SellerReplyMaxLength = 500;

    // Forward-only moderation table: Pending is decided once, approved content
    // can still be taken down later (abuse/policy), and Rejected is terminal -
    // an appeal is a new review, so a rejection can never be silently undone.
    private static readonly IReadOnlyDictionary<ReviewStatus, ReviewStatus[]> AllowedTransitions =
        new Dictionary<ReviewStatus, ReviewStatus[]>
        {
            [ReviewStatus.Pending]   = [ReviewStatus.Approved, ReviewStatus.Rejected],
            [ReviewStatus.Approved] = [ReviewStatus.Rejected],
            [ReviewStatus.Rejected] = [],
        };

    public Guid ProductId { get; private set; }
    public Guid CustomerId { get; private set; }

    // Set when the review is tied to an actual purchase. Derived, never
    // EF-mapped - same call as Order.GrandTotal.
    public Guid? OrderId { get; private set; }
    public bool IsVerifiedPurchase => OrderId.HasValue;

    public int Rating { get; private set; }
    public string? Title { get; private set; }
    public string Comment { get; private set; } = string.Empty;

    public ReviewStatus Status { get; private set; }

    // Set on rejection/takedown; kept for moderation audit
    // (same rule as Shipment.FailureReason / ReturnRequest.RejectionReason).
    public string? ModerationReason { get; private set; }
    public DateTimeOffset? ModeratedAtUtc { get; private set; }

    // Merchant's answer, only on published reviews.
    public string? SellerReply { get; private set; }
    public DateTimeOffset? RepliedAtUtc { get; private set; }

    private ProductReview() { } // required by EF Core

    private ProductReview(
        Guid productId,
        Guid customerId,
        Guid? orderId,
        int rating,
        string? title,
        string comment)
    {
        ProductId = productId;
        CustomerId = customerId;
        OrderId = orderId;
        Rating = rating;
        Title = title;
        Comment = comment;
        Status = ReviewStatus.Pending;
        CreatedAtUtc = UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public static ProductReview Create(
        Guid productId,
        Guid customerId,
        int rating,
        string comment,
        string? title = null,
        Guid? orderId = null)
    {
        if (productId == Guid.Empty)
            throw new DomainException("Product ID is required.");

        if (customerId == Guid.Empty)
            throw new DomainException("Customer ID is required.");

        if (orderId == Guid.Empty)
            throw new DomainException("Order ID cannot be empty when provided.");

        if (rating is < RatingMin or > RatingMax)
            throw new DomainException(
                $"Rating must be between {RatingMin} and {RatingMax}.");

        return new ProductReview(
            productId,
            customerId,
            orderId,
            rating,
            Optional(title, "Title", TitleMaxLength),
            Required(comment, "Comment", CommentMaxLength));
    }

    // Moderator accepts the review - it becomes visible on the product page.
    public void Approve()
    {
        if (Status == ReviewStatus.Approved)
            return; // duplicate moderation call: already there

        MoveTo(ReviewStatus.Approved);
        ModeratedAtUtc = DateTimeOffset.UtcNow;
    }

    // Moderator declines a new review or takes down published content
    // (spam, abuse, policy). The reason is kept for the audit trail.
    public void Reject(string reason)
    {
        var trimmedReason = Required(reason, "Moderation reason", ModerationReasonMaxLength);

        if (Status == ReviewStatus.Rejected)
            return; // duplicate moderation call: original reason kept

        MoveTo(ReviewStatus.Rejected);
        ModerationReason = trimmedReason;
        ModeratedAtUtc = DateTimeOffset.UtcNow;
    }

    // Merchant answers the review - only content that is actually published
    // can be answered, and re-replying updates the reply.
    public void Reply(string reply)
    {
        if (Status != ReviewStatus.Approved)
            throw new DomainException("Only approved reviews can receive a reply.");

        SellerReply = Required(reply, "Seller reply", SellerReplyMaxLength);
        RepliedAtUtc = DateTimeOffset.UtcNow;
        Touch();
    }

    // ---------- Transition machine + validation ----------

    private void MoveTo(ReviewStatus target)
    {
        if (!AllowedTransitions[Status].Contains(target))
            throw new DomainException(
                $"Product review cannot move from {Status} to {target}.");

        Status = target;
        Touch();
    }

    private void Touch() => UpdatedAtUtc = DateTimeOffset.UtcNow;

    private static string Required(string value, string field, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException($"{field} is required.");

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
            throw new DomainException($"{field} cannot exceed {maxLength} characters.");

        return trimmed;
    }

    // Optional text: null/whitespace stays null, anything else is trimmed
    // and length-checked (same rule as Shipment.Create's service level).
    private static string? Optional(string? value, string field, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
            throw new DomainException($"{field} cannot exceed {maxLength} characters.");

        return trimmed;
    }
}
