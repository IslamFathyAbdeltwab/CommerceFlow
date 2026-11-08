using CommerceFlow.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommerceFlow.Infrastructure.Persistence.Configurations.Catalog;

internal sealed class ProductAttributeConfiguration : IEntityTypeConfiguration<ProductAttribute>
{
    public void Configure(EntityTypeBuilder<ProductAttribute> builder)
    {
        builder.ToTable("ProductAttributes");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Name)
            .IsRequired()
            .HasMaxLength(ProductAttribute.NameMaxLength);

        builder.Property(a => a.Code)
            .IsRequired()
            .HasMaxLength(ProductAttribute.CodeMaxLength);

        builder.HasIndex(a => a.Code).IsUnique();

        builder.Property(a => a.DataType).IsRequired();
    }
}
