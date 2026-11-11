using CommerceFlow.Domain.Entities.Carts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommerceFlow.Infrastructure.Persistence.Configurations.Carts;

internal sealed class CartConfiguration : IEntityTypeConfiguration<Cart>
{
    public void Configure(EntityTypeBuilder<Cart> builder)
    {
        builder.ToTable("Carts");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.CustomerId).IsRequired();

        // Belt-and-braces: at most one Active cart per customer at the database
        // level. CreateCartCommandHandler pre-checks for the friendly 409;
        // this filtered index closes the race between two concurrent creates.
        builder.HasIndex(c => c.CustomerId)
            .IsUnique()
            .HasFilter("[Status] = 0"); // CartStatus.Active == 0

        builder.Property(c => c.Status).IsRequired();

        // LineCount and TotalQuantity are derived in the domain and deliberately not mapped.

        builder.HasMany(c => c.Lines)
            .WithOne()
            .HasForeignKey(l => l.CartId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(c => c.Lines).HasField("_lines").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class CartLineConfiguration : IEntityTypeConfiguration<CartLine>
{
    public void Configure(EntityTypeBuilder<CartLine> builder)
    {
        builder.ToTable("CartLines");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.CartId).IsRequired();

        // Variant is a Catalog aggregate - reference by ID only (FK enforced in DB).
        builder.Property(l => l.ProductVariantId).IsRequired();
        builder.HasIndex(l => l.ProductVariantId);

        // The domain merges duplicate variants into one line - back it with a unique index.
        builder.HasIndex(l => new { l.CartId, l.ProductVariantId }).IsUnique();

        builder.Property(l => l.Quantity).IsRequired();
        builder.Property(l => l.AddedAtUtc).IsRequired();

        // No price/SKU snapshot on purpose: a cart always shows live catalog data.
    }
}
