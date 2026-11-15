using CommerceFlow.Domain.Entities.Promotions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommerceFlow.Infrastructure.Persistence.Configurations.Promotions;

internal sealed class PromotionConfiguration : IEntityTypeConfiguration<Promotion>
{
    public void Configure(EntityTypeBuilder<Promotion> builder)
    {
        builder.ToTable("Promotions");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Code)
            .IsRequired()
            .HasMaxLength(Promotion.CodeMaxLength);
        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(Promotion.NameMaxLength);
        builder.Property(p => p.Description)
            .HasMaxLength(Promotion.DescriptionMaxLength);

        builder.Property(p => p.DiscountType).IsRequired();
        builder.Property(p => p.DiscountValue).HasPrecision(18, 2);
        builder.Property(p => p.Status).IsRequired();

        builder.Property(p => p.StartAtUtc).IsRequired();
        builder.Property(p => p.MinOrderAmount).HasPrecision(18, 2);

        // the uniqueness backstop behind the create handler's pre-check
        // (case-insensitive via the database collation, like the pre-check)
        builder.HasIndex(p => p.Code).IsUnique();

        // lookup index for the list endpoint's status filter
        builder.HasIndex(p => p.Status);

        builder.HasMany(p => p.Usages)
            .WithOne()
            .HasForeignKey(u => u.PromotionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(p => p.Usages)
            .HasField("_usages")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class PromotionUsageConfiguration : IEntityTypeConfiguration<PromotionUsage>
{
    public void Configure(EntityTypeBuilder<PromotionUsage> builder)
    {
        builder.ToTable("PromotionUsages");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.PromotionId).IsRequired();
        builder.Property(u => u.CustomerId).IsRequired();
        builder.Property(u => u.OrderId).IsRequired();
        builder.Property(u => u.DiscountTaken).HasPrecision(18, 2);
        builder.Property(u => u.UsedAtUtc).IsRequired();

        // lookup indexes: the per-customer limit counts by customer, the
        // audit reads by campaign
        builder.HasIndex(u => u.PromotionId);
        builder.HasIndex(u => u.CustomerId);

        // one redemption per order per campaign - the backstop behind the
        // domain's duplicate-order guard
        builder.HasIndex(u => new { u.PromotionId, u.OrderId }).IsUnique();
    }
}
