using CommerceFlow.Domain.Entities.Reviews;
using CommerceFlow.Domain.Enums;

namespace CommerceFlow.Application.Reviews.Queries;

// One DTO for detail and list reads: moderation state and the seller reply
// ride along because the product page shows them (same call as PaymentDto).
public sealed record ProductReviewDto(
    Guid Id,
    Guid ProductId,
    Guid CustomerId,
    Guid? OrderId,
    bool IsVerifiedPurchase, // derived from OrderId - never stored twice
    int Rating,
    string? Title,
    string Comment,
    ReviewStatus Status,
    string? ModerationReason,
    DateTimeOffset? ModeratedAtUtc,
    string? SellerReply,
    DateTimeOffset? RepliedAtUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

// Centralizes entity -> DTO mapping so the handlers stay thin.
internal static class ProductReviewMappings
{
    public static ProductReviewDto ToDto(ProductReview review) => new(
        review.Id,
        review.ProductId,
        review.CustomerId,
        review.OrderId,
        review.IsVerifiedPurchase,
        review.Rating,
        review.Title,
        review.Comment,
        review.Status,
        review.ModerationReason,
        review.ModeratedAtUtc,
        review.SellerReply,
        review.RepliedAtUtc,
        review.CreatedAtUtc,
        review.UpdatedAtUtc);
}
