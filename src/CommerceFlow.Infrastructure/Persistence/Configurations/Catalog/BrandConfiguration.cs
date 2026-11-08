using CommerceFlow.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommerceFlow.Infrastructure.Persistence.Configurations.Catalog;

internal sealed class BrandConfiguration : IEntityTypeConfiguration<Brand>
{
    public void Configure(EntityTypeBuilder<Brand> builder)
    {
        builder.ToTable("Brands");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Name)
            .IsRequired()
            .HasMaxLength(Brand.NameMaxLength);

        builder.Property(b => b.Slug)
            .IsRequired()
            .HasMaxLength(Brand.SlugMaxLength);

        builder.HasIndex(b => b.Slug).IsUnique();

        builder.Property(b => b.Description)
            .HasMaxLength(Brand.DescriptionMaxLength);

        builder.Property(b => b.LogoUrl)
            .HasMaxLength(Brand.LogoUrlMaxLength);

        builder.Property(b => b.IsActive).IsRequired();
    }
}
