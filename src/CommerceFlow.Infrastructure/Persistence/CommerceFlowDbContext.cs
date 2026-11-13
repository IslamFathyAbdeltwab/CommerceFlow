using CommerceFlow.Domain.Entities.Addresses;
using CommerceFlow.Domain.Entities.Carts;
using CommerceFlow.Domain.Entities.Catalog;
using CommerceFlow.Domain.Entities.Inventory;
using CommerceFlow.Domain.Entities.Orders;
using CommerceFlow.Domain.Entities.Payments;
using CommerceFlow.Domain.Entities.Returns;
using CommerceFlow.Domain.Entities.Reviews;
using CommerceFlow.Domain.Entities.Shipping;
using CommerceFlow.Domain.Entities.Wishlists;
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

    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<InventoryReservation> InventoryReservations => Set<InventoryReservation>();
    public DbSet<InventoryReservationItem> InventoryReservationItems => Set<InventoryReservationItem>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();

    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartLine> CartLines => Set<CartLine>();

    public DbSet<Wishlist> Wishlists => Set<Wishlist>();
    public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();

    public DbSet<CustomerAddress> CustomerAddresses => Set<CustomerAddress>();

    public DbSet<Payment> Payments => Set<Payment>();

    public DbSet<Shipment> Shipments => Set<Shipment>();
    public DbSet<ShipmentItem> ShipmentItems => Set<ShipmentItem>();

    public DbSet<ReturnRequest> ReturnRequests => Set<ReturnRequest>();
    public DbSet<ReturnItem> ReturnItems => Set<ReturnItem>();

    public DbSet<ProductReview> ProductReviews => Set<ProductReview>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CommerceFlowDbContext).Assembly);

        // Domain generates Guids (Entity base does Guid.NewGuid()).
        // Tell EF keys are client-generated so new children (ProductVariant, ProductImage)
        // discovered via aggregate navigations are INSERTed, not UPDATE-d (DbUpdateConcurrency).
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var idProperty = entityType.FindProperty("Id");
            if (idProperty is not null && idProperty.ClrType == typeof(Guid))
            {
                idProperty.ValueGenerated = Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.Never;
            }
        }
    }
}
