using CommerceFlow.Domain.Entities.Wishlists;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommerceFlow.Infrastructure.Persistence.Configurations.Wishlists;

internal sealed class WishlistConfiguration : IEntityTypeConfiguration<Wishlist>
{
    public void Configure(EntityTypeBuilder<Wishlist> builder)
    {
        builder.ToTable("Wishlists");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.CustomerId).IsRequired();

        // One wishlist per customer - unique at the database level.
        // CreateWishlistCommandHandler pre-checks for the friendly 409;
        // this index closes the race between two concurrent creates.
        builder.HasIndex(w => w.CustomerId).IsUnique();

        // ItemCount is derived in the domain and deliberately not mapped.

        builder.HasMany(w => w.Items)
            .WithOne()
            .HasForeignKey(i => i.WishlistId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(w => w.Items).HasField("_items").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class WishlistItemConfiguration : IEntityTypeConfiguration<WishlistItem>
{
    public void Configure(EntityTypeBuilder<WishlistItem> builder)
    {
        builder.ToTable("WishlistItems");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.WishlistId).IsRequired();

        // Variant is a Catalog aggregate - reference by ID only (index, no cross-module FK;
        // same decision as Cart lines and Order lines).
        builder.Property(i => i.ProductVariantId).IsRequired();
        builder.HasIndex(i => i.ProductVariantId);

        // Add is idempotent in the domain (one item per variant) - back it with a unique index.
        builder.HasIndex(i => new { i.WishlistId, i.ProductVariantId }).IsUnique();

        builder.Property(i => i.AddedAtUtc).IsRequired();

        // No quantity/price snapshot on purpose: a wishlist always shows live catalog data.
    }
}
