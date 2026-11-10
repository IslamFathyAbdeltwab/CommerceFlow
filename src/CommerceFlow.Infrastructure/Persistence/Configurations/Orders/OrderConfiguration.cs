using CommerceFlow.Domain.Entities.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommerceFlow.Infrastructure.Persistence.Configurations.Orders;

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.OrderNumber)
            .IsRequired()
            .HasMaxLength(30);

        builder.HasIndex(o => o.OrderNumber).IsUnique();

        builder.Property(o => o.CustomerId).IsRequired();
        builder.HasIndex(o => o.CustomerId);

        builder.Property(o => o.Status).IsRequired();
        builder.Property(o => o.PaymentStatus).IsRequired();

        builder.Property(o => o.Currency)
            .IsRequired()
            .HasMaxLength(Order.CurrencyLength);

        builder.Property(o => o.Subtotal).HasPrecision(18, 2);
        builder.Property(o => o.ShippingFee).HasPrecision(18, 2);
        builder.Property(o => o.DiscountAmount).HasPrecision(18, 2);

        builder.Property(o => o.CustomerNote).HasMaxLength(Order.CustomerNoteMaxLength);
        builder.Property(o => o.CancelReason).HasMaxLength(Order.CancelReasonMaxLength);

        // GrandTotal is derived in the domain and deliberately not mapped.

        // Immutable address snapshot as owned columns (ShippingAddress_*),
        // so editing a customer's saved address never rewrites old orders.
        builder.OwnsOne(o => o.ShippingAddress, address =>
        {
            address.Property(a => a.FullName)
                .HasColumnName("ShippingAddress_FullName")
                .IsRequired()
                .HasMaxLength(AddressSnapshot.FullNameMaxLength);

            address.Property(a => a.Line1)
                .HasColumnName("ShippingAddress_Line1")
                .IsRequired()
                .HasMaxLength(AddressSnapshot.LineMaxLength);

            address.Property(a => a.Line2)
                .HasColumnName("ShippingAddress_Line2")
                .HasMaxLength(AddressSnapshot.LineMaxLength);

            address.Property(a => a.City)
                .HasColumnName("ShippingAddress_City")
                .IsRequired()
                .HasMaxLength(AddressSnapshot.CityMaxLength);

            address.Property(a => a.State)
                .HasColumnName("ShippingAddress_State")
                .HasMaxLength(AddressSnapshot.StateMaxLength);

            address.Property(a => a.PostalCode)
                .HasColumnName("ShippingAddress_PostalCode")
                .IsRequired()
                .HasMaxLength(AddressSnapshot.PostalCodeMaxLength);

            address.Property(a => a.Country)
                .HasColumnName("ShippingAddress_Country")
                .IsRequired()
                .HasMaxLength(AddressSnapshot.CountryMaxLength);

            address.Property(a => a.PhoneNumber)
                .HasColumnName("ShippingAddress_PhoneNumber")
                .HasMaxLength(AddressSnapshot.PhoneNumberMaxLength);
        });

        builder.HasMany(o => o.Lines)
            .WithOne()
            .HasForeignKey(l => l.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(o => o.Lines).HasField("_lines").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class OrderLineConfiguration : IEntityTypeConfiguration<OrderLine>
{
    public void Configure(EntityTypeBuilder<OrderLine> builder)
    {
        builder.ToTable("OrderLines");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.OrderId).IsRequired();

        // Variant is a Catalog aggregate - reference by ID only.
        builder.Property(l => l.ProductVariantId).IsRequired();
        builder.HasIndex(l => l.ProductVariantId);

        builder.Property(l => l.Sku)
            .IsRequired()
            .HasMaxLength(OrderLine.SkuMaxLength);

        builder.Property(l => l.ProductName)
            .IsRequired()
            .HasMaxLength(OrderLine.ProductNameMaxLength);

        builder.Property(l => l.UnitPrice).HasPrecision(18, 2);
        builder.Property(l => l.Quantity).IsRequired();

        // LineTotal is derived in the domain and deliberately not mapped.
    }
}
