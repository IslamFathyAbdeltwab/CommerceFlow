using CommerceFlow.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommerceFlow.Infrastructure.Persistence.Configurations.Catalog;

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(Category.NameMaxLength);

        builder.Property(c => c.Slug)
            .IsRequired()
            .HasMaxLength(Category.SlugMaxLength);

        builder.HasIndex(c => c.Slug).IsUnique();

        builder.Property(c => c.Description)
            .HasMaxLength(Category.DescriptionMaxLength);

        builder.HasIndex(c => c.ParentCategoryId);
    }
}
