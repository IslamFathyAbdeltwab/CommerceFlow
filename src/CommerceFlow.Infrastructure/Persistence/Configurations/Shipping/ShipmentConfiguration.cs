using CommerceFlow.Domain.Entities.Shipping;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommerceFlow.Infrastructure.Persistence.Configurations.Shipping;

internal sealed class ShipmentConfiguration : IEntityTypeConfiguration<Shipment>
{
    public void Configure(EntityTypeBuilder<Shipment> builder)
    {
        builder.ToTable("Shipments");

        builder.HasKey(s => s.Id);

        // ID-only references (no navigations), same as Payment/Order.
        builder.Property(s => s.OrderId).IsRequired();

        builder.Property(s => s.Status).IsRequired();
        builder.Property(s => s.Carrier)
            .IsRequired()
            .HasMaxLength(Shipment.CarrierMaxLength);
        builder.Property(s => s.TrackingNumber)
            .IsRequired()
            .HasMaxLength(Shipment.TrackingNumberMaxLength);
        builder.Property(s => s.ServiceLevel)
            .HasMaxLength(Shipment.ServiceLevelMaxLength);
        builder.Property(s => s.FailureReason)
            .HasMaxLength(Shipment.FailureReasonMaxLength);

        // Lookup indexes for the list endpoint's filters.
        builder.HasIndex(s => s.OrderId);
        builder.HasIndex(s => s.Status);

        // Carrier/support lookups assume one shipment per tracking number -
        // two boxes sharing it would make a lookup ambiguous.
        builder.HasIndex(s => s.TrackingNumber).IsUnique();

        builder.HasMany(s => s.Items)
            .WithOne()
            .HasForeignKey(i => i.ShipmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(s => s.Items).HasField("_items").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class ShipmentItemConfiguration : IEntityTypeConfiguration<ShipmentItem>
{
    public void Configure(EntityTypeBuilder<ShipmentItem> builder)
    {
        builder.ToTable("ShipmentItems");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.ShipmentId).IsRequired();
        builder.Property(i => i.ProductVariantId).IsRequired();
        builder.Property(i => i.Quantity).IsRequired();

        // one line per variant inside a box (the domain rejects duplicates)
        builder.HasIndex(i => new { i.ShipmentId, i.ProductVariantId }).IsUnique();
        builder.HasIndex(i => i.ProductVariantId);
    }
}
