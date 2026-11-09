using CommerceFlow.Domain.Entities.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommerceFlow.Infrastructure.Persistence.Configurations.Inventory;

internal sealed class InventoryReservationConfiguration : IEntityTypeConfiguration<InventoryReservation>
{
    public void Configure(EntityTypeBuilder<InventoryReservation> builder)
    {
        builder.ToTable("InventoryReservations");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.OrderId).IsRequired();
        builder.HasIndex(r => r.OrderId).IsUnique();
        builder.HasIndex(r => r.Status);
        builder.HasIndex(r => r.ExpiresAtUtc);

        builder.Property(r => r.Status).IsRequired();

        builder.HasMany(r => r.Items)
            .WithOne()
            .HasForeignKey(i => i.InventoryReservationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(r => r.Items).HasField("_items").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class InventoryReservationItemConfiguration : IEntityTypeConfiguration<InventoryReservationItem>
{
    public void Configure(EntityTypeBuilder<InventoryReservationItem> builder)
    {
        builder.ToTable("InventoryReservationItems");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.InventoryReservationId).IsRequired();
        builder.Property(i => i.WarehouseId).IsRequired();
        builder.Property(i => i.ProductVariantId).IsRequired();
        builder.Property(i => i.Quantity).IsRequired();

        builder.HasIndex(i => new { i.InventoryReservationId, i.WarehouseId, i.ProductVariantId }).IsUnique();
        builder.HasIndex(i => i.WarehouseId);
        builder.HasIndex(i => i.ProductVariantId);
    }
}
