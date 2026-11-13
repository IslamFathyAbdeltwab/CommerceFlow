using CommerceFlow.Domain.Entities.Reviews;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommerceFlow.Infrastructure.Persistence.Configurations.Reviews;

internal sealed class ProductReviewConfiguration : IEntityTypeConfiguration<ProductReview>
{
    public void Configure(EntityTypeBuilder<ProductReview> builder)
    {
        builder.ToTable("ProductReviews");

        builder.HasKey(r => r.Id);

        // ID-only references (no navigations), same as Payment/Order/Shipment.
        builder.Property(r => r.ProductId).IsRequired();
        builder.Property(r => r.CustomerId).IsRequired();
        // OrderId is optional: null = not a verified purchase.
        // IsVerifiedPurchase is derived in the domain and deliberately not
        // mapped (same call as Order.GrandTotal).

        builder.Property(r => r.Rating).IsRequired();
        builder.Property(r => r.Title)
            .HasMaxLength(ProductReview.TitleMaxLength);
        builder.Property(r => r.Comment)
            .IsRequired()
            .HasMaxLength(ProductReview.CommentMaxLength);

        builder.Property(r => r.Status).IsRequired();
        builder.Property(r => r.ModerationReason)
            .HasMaxLength(ProductReview.ModerationReasonMaxLength);
        builder.Property(r => r.SellerReply)
            .HasMaxLength(ProductReview.SellerReplyMaxLength);

        // Lookup indexes for the list endpoint's filters.
        builder.HasIndex(r => r.ProductId);
        builder.HasIndex(r => r.CustomerId);

        // One review per customer per product, enforced at the database too -
        // the handler's 409 pre-check is the friendly error, this is the
        // backstop under concurrency (same call as the unique tracking number).
        builder.HasIndex(r => new { r.ProductId, r.CustomerId }).IsUnique();
    }
}
