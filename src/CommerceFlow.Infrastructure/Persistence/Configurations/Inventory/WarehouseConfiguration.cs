using CommerceFlow.Domain.Entities.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommerceFlow.Infrastructure.Persistence.Configurations.Inventory;

internal sealed class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.ToTable("Warehouses");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.Code)
            .IsRequired()
            .HasMaxLength(Warehouse.CodeMaxLength);

        builder.HasIndex(w => w.Code).IsUnique();

        builder.Property(w => w.Name)
            .IsRequired()
            .HasMaxLength(Warehouse.NameMaxLength);

        builder.Property(w => w.Description)
            .HasMaxLength(Warehouse.DescriptionMaxLength);

        builder.Property(w => w.IsActive).IsRequired();
    }
}
