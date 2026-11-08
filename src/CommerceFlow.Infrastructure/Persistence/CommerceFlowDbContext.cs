using CommerceFlow.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;

namespace CommerceFlow.Infrastructure.Persistence;

// Single DbContext for the whole application (modular monolith).
// Module-specific configurations live in Persistence/Configurations/*.
public sealed class CommerceFlowDbContext(DbContextOptions<CommerceFlowDbContext> options)
    : DbContext(options)
{
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<ProductAttribute> ProductAttributes => Set<ProductAttribute>();
    public DbSet<ProductVariantAttributeValue> ProductVariantAttributeValues => Set<ProductVariantAttributeValue>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CommerceFlowDbContext).Assembly);
    }
}
