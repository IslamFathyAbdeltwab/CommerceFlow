using CommerceFlow.Domain.Entities.Catalog;
using CommerceFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommerceFlow.Infrastructure.Persistence.Configurations.Catalog;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(Product.NameMaxLength);

        builder.Property(p => p.Slug)
            .IsRequired()
            .HasMaxLength(Product.SlugMaxLength);

        builder.HasIndex(p => p.Slug).IsUnique();

        builder.Property(p => p.Description).IsRequired();
        builder.Property(p => p.Status).IsRequired();

        builder.HasIndex(p => p.Status);
        builder.HasIndex(p => p.IsFeatured);
        builder.HasIndex(p => p.BrandId);

        // Brand is a separate aggregate: Product references it by ID only (no EF navigation).
        builder.HasOne<Brand>()
            .WithMany()
            .HasForeignKey(p => p.BrandId)
            .OnDelete(DeleteBehavior.SetNull);

        // Product is the aggregate root for Variants, Images and Categories (inside-aggregate navigations).
        // HasMany is required so EF tracks new children as Added (fix DbUpdateConcurrencyException).
        builder.HasMany(p => p.Variants)
            .WithOne()
            .HasForeignKey(v => v.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Images)
            .WithOne()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Categories)
            .WithOne()
            .HasForeignKey(pc => pc.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(p => p.Variants).HasField("_variants").UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(p => p.Images).HasField("_images").UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(p => p.Categories).HasField("_categories").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.ToTable("ProductVariants");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.Name)
            .IsRequired()
            .HasMaxLength(ProductVariant.NameMaxLength);

        builder.Property(v => v.Sku)
            .IsRequired()
            .HasMaxLength(ProductVariant.SkuMaxLength);

        builder.HasIndex(v => v.Sku).IsUnique();

        builder.Property(v => v.Barcode)
            .HasMaxLength(ProductVariant.BarcodeMaxLength);

        builder.HasIndex(v => v.Barcode);

        builder.Property(v => v.Price)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(v => v.CompareAtPrice)
            .HasPrecision(18, 2);

        builder.Property(v => v.WeightGrams);
        builder.Property(v => v.IsActive).IsRequired();

        builder.HasIndex(v => v.ProductId);
        builder.HasIndex(v => v.IsActive);

        builder.HasMany(v => v.AttributeValues)
            .WithOne()
            .HasForeignKey(av => av.ProductVariantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(v => v.AttributeValues).HasField("_attributeValues").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.ToTable("ProductImages");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.StorageKey)
            .IsRequired()
            .HasMaxLength(ProductImage.StorageKeyMaxLength);

        builder.Property(i => i.AltText)
            .HasMaxLength(ProductImage.AltTextMaxLength);

        builder.HasIndex(i => i.ProductId);
        builder.HasIndex(i => i.ProductVariantId);
    }
}

internal sealed class ProductCategoryConfiguration : IEntityTypeConfiguration<ProductCategory>
{
    public void Configure(EntityTypeBuilder<ProductCategory> builder)
    {
        builder.ToTable("ProductCategories");

        // Composite key: one row per (Product, Category) pair.
        builder.HasKey(pc => new { pc.ProductId, pc.CategoryId });

        builder.Property(pc => pc.AssignedAtUtc).IsRequired();

        builder.HasIndex(pc => pc.CategoryId);

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(pc => pc.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ProductVariantAttributeValueConfiguration : IEntityTypeConfiguration<ProductVariantAttributeValue>
{
    public void Configure(EntityTypeBuilder<ProductVariantAttributeValue> builder)
    {
        builder.ToTable("ProductVariantAttributeValues");

        // Composite key: one value per (Variant, Attribute) pair.
        builder.HasKey(v => new { v.ProductVariantId, v.ProductAttributeId });

        builder.Property(v => v.Value)
            .IsRequired()
            .HasMaxLength(ProductVariantAttributeValue.ValueMaxLength);

        builder.HasOne<ProductAttribute>()
            .WithMany()
            .HasForeignKey(v => v.ProductAttributeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
