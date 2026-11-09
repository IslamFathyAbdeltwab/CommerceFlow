using CommerceFlow.Domain.Entities.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommerceFlow.Infrastructure.Persistence.Configurations.Inventory;

internal sealed class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("StockMovements");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.WarehouseId).IsRequired();
        builder.Property(m => m.ProductVariantId).IsRequired();
        builder.Property(m => m.MovementType).IsRequired();
        builder.Property(m => m.QuantityChange).IsRequired();
        builder.Property(m => m.QuantityAfter);

        builder.Property(m => m.ReferenceType).HasMaxLength(100);
        builder.Property(m => m.ReferenceId);
        builder.Property(m => m.Reason).IsRequired().HasMaxLength(500);
        builder.Property(m => m.PerformedByUserId);
        builder.Property(m => m.CreatedAtUtcMovement).IsRequired();

        builder.HasIndex(m => new { m.WarehouseId, m.ProductVariantId });
        builder.HasIndex(m => m.MovementType);
        builder.HasIndex(m => m.ReferenceId);
        builder.HasIndex(m => m.CreatedAtUtcMovement);
    }
}
