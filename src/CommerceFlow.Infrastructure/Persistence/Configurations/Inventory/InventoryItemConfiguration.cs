using CommerceFlow.Domain.Entities.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommerceFlow.Infrastructure.Persistence.Configurations.Inventory;

internal sealed class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        builder.ToTable("InventoryItems");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.WarehouseId).IsRequired();
        builder.Property(i => i.ProductVariantId).IsRequired();

        builder.HasIndex(i => new { i.WarehouseId, i.ProductVariantId }).IsUnique();

        builder.HasIndex(i => i.WarehouseId);
        builder.HasIndex(i => i.ProductVariantId);

        builder.Property(i => i.OnHandQuantity).IsRequired();
        builder.Property(i => i.ReservedQuantity).IsRequired();
        builder.Property(i => i.ReorderThreshold).IsRequired();

        // Optimistic concurrency for concurrent reservations (no oversell).
        builder.Property(i => i.RowVersion)
            .IsRowVersion()
            .HasColumnType("rowversion");
    }
}
